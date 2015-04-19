using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AssemblyLoader;
using Injector.DTO;
using Injector.DTO.Discovery;
using Injector.DTO.Messaging;
using Injector.InjectorHost.Extensions;
using SocketHelpers.Discovery;
using SocketHelpers.Messaging;
using Sockets.Plugin;
using Sockets.Plugin.Abstractions;
using Splat;

namespace Injector.InjectorHost
{
    public abstract class InjectorHostBase<TProxy, TMessage> : IEnableLogger
        where TMessage : Message
        where TProxy : InjectorClientProxy, new()
    {
        private readonly ServicePublisher<TypedServiceDefinition<string, InjectorHostServiceResponse>> _servicePublisher;
        protected readonly MessageHub<TProxy, TMessage> _messageHub = new MessageHub<TProxy, TMessage>();

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private int _servicePort = InjectorDefaults.HostListenPort;
        public string ServiceGuid = Guid.NewGuid().ToString();
        private List<TProxy> _connectedClients = new List<TProxy>();
        private readonly bool _forceCanInject;

        public bool IsListening { get; private set; }

        public int ServicePort
        {
            get { return _servicePort; }
            set { _servicePort = value; }
        }

        public List<TProxy> ConnectedClients
        {
            get { return _connectedClients; }
            set { _connectedClients = value; }
        }

        protected InjectorHostBase(bool forceCanInject = false)
        {
            _forceCanInject = forceCanInject;

            var dtoAssembly = typeof (NewAssemblyMessage).GetTypeInfo().Assembly;
            _messageHub.AdditionalTypeResolutionAssemblies.Add(dtoAssembly);

            // set up the discovery responder
            _servicePublisher = new InjectorServiceDefinition(async () =>
            {
                // get the current interfaces
                var ifs = await CommsInterface.GetAllInterfacesAsync();
                var usable = ifs.Where(iface => iface.IsUsable && !iface.IsLoopback)
                    .Where(iface => iface.ConnectionStatus == CommsInterfaceStatus.Connected).ToList();

                // if none (how :O), we won't respond. how could we event
                if (!usable.Any())
                    return null;

                return new InjectorHostServiceResponse
                {
                    ServiceGuid = ServiceGuid,
                    ServiceName = GetType().FullName,
                    Port = ServicePort,
                    NumConnectedClients = ConnectedClients.Count,
                    RunningAt = ifs.Select(iface => String.Join(":", iface.IpAddress, ServicePort.ToString())).ToList()
                };
            }).CreateServicePublisher();
        }

        public virtual async Task StartHostingAsync()
        {
            // check if we can act as an injection host
            if (!Loader.RuntimeLoadAvailable && !_forceCanInject)
                throw new InvalidOperationException(
                    "The current platform does not support loading assemblies at runtime.");

            // start publishing host availability if we have a service definition
            if (_servicePublisher != null)
            {
                await _servicePublisher.Publish();
            }

            // wire up our client connected/disconnected methods 
            _messageHub.ClientConnected.Subscribe(ClientConnected);
            _messageHub.ClientDisconnected.Subscribe(ClientDisconnected);

            // funnel any messages containing assemblies through the pipeline 
            _messageHub
                .AllMessages
                .OfType<IMessageWithAssemblyBytes>()
                .Where(ShouldLoadAssembly)
                .Select(msg =>
                {
                    var proxy = _messageHub.ProxyForGuid(msg.FromGuid);

                    try
                    {
                        return new {Assembly = BytesToAssembly(msg.AssemblyBytes), Sender = proxy};
                    }
                    catch (Exception e)
                    {
                        this.Log()
                            .Error(String.Format("Couldn't load assembly named {0} from {1}: '{2}'", msg.AssemblyName,
                                proxy, e.Message));
                    }

                    return null;
                })
                .Where(a => a != null)
                .Subscribe(x => ProcessNewAssembly(x.Assembly, x.Sender))
                .DisposeWith(_disposables);

            // start listening
            await _messageHub.StartListeningAsync(ServicePort);

            IsListening = true;
        }

        public virtual async Task StopHostingAsync()
        {
            IsListening = false;

            // stop being available to service discoverers
            await _servicePublisher.Unpublish();

            // stop accepting new connections
            await _messageHub.StopListeningAsync();

            // send a shutdown message
            // TODO: ...

            // disconnect everyone
            await _messageHub.DisconnectAllClients();
        }

        protected virtual void ClientConnected(TProxy proxy)
        {
            ConnectedClients.Add(proxy);
        }

        protected virtual void ClientDisconnected(TProxy proxy)
        {
            ConnectedClients.Remove(proxy);
        }

        protected virtual bool ShouldLoadAssembly(IMessageWithAssemblyBytes msg)
        {
            return true;
        }

        protected virtual Assembly BytesToAssembly(byte[] rawAssembly)
        {
            return Loader.LoadAssembly(rawAssembly);
        }

        protected Task SendFeedback(TMessage e, TProxy proxy = null)
        {
            return proxy != null ? _messageHub.SendToAsync(e, proxy) : _messageHub.SendAllAsync(e);
        }

        protected abstract void ProcessNewAssembly(Assembly newAssembly, TProxy sender);
    }
}