using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Injector.DTO;
using Injector.DTO.Discovery;
using Injector.InjectorClient;
using ReactiveUI;
using SocketHelpers.Discovery;

namespace InjectorClientWPF
{
    public class InjectorClientViewModel : ReactiveObject
    {
        private readonly FilesystemInjectorClient _injectorClient = new FilesystemInjectorClient();

        private string _connectToAddress;

        public string ConnectToAddress
        {
            get { return _connectToAddress; }
            set { this.RaiseAndSetIfChanged(ref _connectToAddress, value); }
        }

        private string _connectToPort;

        public string ConnectToPort
        {
            get { return _connectToPort; }
            set { this.RaiseAndSetIfChanged(ref _connectToPort, value); }
        }

        private string _watchPath;

        public string WatchPath
        {
            get { return _watchPath; }
            set { this.RaiseAndSetIfChanged(ref _watchPath, value); }
        }

        private string _watchMask;

        public string WatchMask
        {
            get { return _watchMask; }
            set { this.RaiseAndSetIfChanged(ref _watchMask, value); }
        }

        private readonly ObservableAsPropertyHelper<bool> _isWatching;

        public bool IsWatching
        {
            get { return _isWatching.Value; }
        }

        private readonly ObservableAsPropertyHelper<bool> _isConnected;

        public bool IsConnected
        {
            get { return _isConnected.Value; }
        }

        private readonly
            ServiceDiscoverer
                <TypedServiceDefinition<string, InjectorHostServiceResponse>, string, InjectorHostServiceResponse>
            _discoverer;

        private IDisposable _discoverySub;

        public ReactiveCommand<bool> Connect { get; set; }
        public ReactiveCommand<bool> Disconnect { get; set; }
        public ReactiveCommand<bool> StartWatching { get; set; }
        public ReactiveCommand<bool> StopWatching { get; set; }

        public ReactiveList<InjectorHostServiceResponse> DiscoveredServiceResponses { get; set; }

        public InjectorClientViewModel()
        {
            WatchMask = "*.dll, *.exe";
            DiscoveredServiceResponses = new ReactiveList<InjectorHostServiceResponse>();

            _discoverer = new InjectorServiceDefinition().CreateServiceDiscoverer();

            // TODO: Use CreateDerivedCollection
            _discoverer
                .DiscoveredServices
                .ObserveOn(SynchronizationContext.Current)
                .Where(svc => !DiscoveredServiceResponses.Any(s => s.ServiceGuid == svc.ServiceGuid))
                .Subscribe(svc => DiscoveredServiceResponses.Add(svc));

            // can watch when valid path in watchpath
            var canStartWatching = this.WhenAnyValue(x => x.WatchPath)
                .Select(x => !String.IsNullOrEmpty(x) && Directory.Exists(x));

            // can connect when address and valid port
            var canConnect = this.WhenAnyValue(x => x.ConnectToAddress, x => x.ConnectToPort,
                (a, p) =>
                    !String.IsNullOrEmpty(a) && !String.IsNullOrEmpty(p) && p.ToCharArray().All(char.IsDigit) &&
                    Int32.Parse(p).IsWithinRange(1, 65536));

            Connect = ReactiveCommand.CreateAsyncTask(canConnect, async _ =>
            {
                await _injectorClient.ConnectAsync(ConnectToAddress, Int32.Parse(ConnectToPort));
                return _injectorClient.IsConnected;
            });

            Disconnect = ReactiveCommand.CreateAsyncTask(async _ =>
            {
                await _injectorClient.DisconnectAsync();
                return _injectorClient.IsConnected;
            });

            StartWatching = ReactiveCommand.CreateAsyncTask(canStartWatching, _ =>
            {
                _injectorClient.StartWatching();
                return Task.FromResult(_injectorClient.IsWatching);
            });

            StopWatching = ReactiveCommand.CreateAsyncTask(_ =>
            {
                _injectorClient.StopWatching();
                return Task.FromResult(_injectorClient.IsWatching);
            });

            // commands return whether or not watching
            Observable
                .Merge(StartWatching, StopWatching)
                .ToProperty(this, x => x.IsWatching, out _isWatching);

            // commands return whether or not connected
            Observable
                .Merge(Connect, Disconnect)
                .ToProperty(this, x => x.IsConnected, out _isConnected);

            // pass through path changes to the injectorclient
            this.WhenAnyValue(x => x.WatchPath)
                .BindTo(_injectorClient, c => c.WatchPath);

            // pass through mask changes to the injectorclient
            this.WhenAnyValue(x => x.WatchMask)
                .BindTo(_injectorClient, c => c.WatchMask);

            // pump out errors
            // TODO: view needs a usererror handler
            Observable.Merge(
                Observable.Merge(Connect.ThrownExceptions, Disconnect.ThrownExceptions),
                Observable.Merge(StartWatching.ThrownExceptions, StopWatching.ThrownExceptions))
                .Subscribe(ex => UserError.Throw("Error", ex));
        }

        public void StartFindingServices()
        {
            // send discovery requests every second
            _discoverySub = Observable
                .Interval(TimeSpan.FromSeconds(1))
                .Subscribe(async _ => await _discoverer.Discover());
        }

        public void StopFindingServices()
        {
            _discoverySub.Dispose();
        }
    }
}