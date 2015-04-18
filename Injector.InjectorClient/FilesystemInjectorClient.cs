using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Injector.DTO.Messaging;
using RxFileSystemWatcher;
using SocketHelpers.Messaging;
using Sockets.Plugin;
using Splat;

namespace Injector.InjectorClient
{
    public class FilesystemInjectorClient : IEnableLogger
    {
        public string WatchMask { get; set; }
        public string WatchPath { get; set; }

        private readonly ObservableFileSystemWatcher _fileSystemWatcher;

        private IObservable<string> ChangedAssemblies
        {
            get
            {
                return Observable.Merge(_fileSystemWatcher.Created, _fileSystemWatcher.Changed)
                    .Where(
                        change =>
                            WatchMask.Split(',')
                                .Select(m => m.Replace("*", "").Trim())
                                .Contains(Path.GetExtension(change.FullPath)))
                    .Select(change => change.FullPath);
            }
        }

        public bool IsWatching { get; set; }
        public bool IsConnected { get; set; }

        private TcpSocketClient _tcpSocketClient = new TcpSocketClient();
        private JsonProtocolMessenger<Message> _jsonProtocolMessenger;
        private CancellationTokenSource _canceller;

        public FilesystemInjectorClient()
        {
            WatchMask = "*.dll, *.exe";
            _fileSystemWatcher = new ObservableFileSystemWatcher(fsw => fsw.IncludeSubdirectories = true);
        }

        public async Task ConnectAsync(string address, int port)
        {
            _canceller = new CancellationTokenSource();

            await _tcpSocketClient.ConnectAsync(address, port);

            _jsonProtocolMessenger = new JsonProtocolMessenger<Message>(_tcpSocketClient);
            _jsonProtocolMessenger.StartExecuting();

            IsConnected = true;
        }

        public void StartWatching()
        {
            if (WatchPath == null || !Directory.Exists(WatchPath))
                throw new InvalidOperationException("Invalid watch path provided");

            _canceller = new CancellationTokenSource();
            _fileSystemWatcher.Watcher.Path = WatchPath;

            ChangedAssemblies
                .GroupBy(ca => ca)
                .SelectMany(ca => ca.Throttle(TimeSpan.FromSeconds(.5)))
                .Subscribe(
                    onNext: f =>
                    {
                        this.Log().Debug(String.Format("{0} changed, attempting to send.. ", f));

                        var bytes = File.ReadAllBytes(f);
                        var msg = new NewAssemblyMessage
                        {
                            AssemblyName = Path.GetFileNameWithoutExtension(f),
                            AssemblyBytes = bytes
                        };

                        _jsonProtocolMessenger.Send(msg);
                    },
                    onError: ex => this.Log().Error(ex.Message),
                    onCompleted: () => { },
                    token: _canceller.Token
                );

            _fileSystemWatcher.Start();

            IsWatching = true;
        }

        public void StopWatching()
        {
            IsWatching = false;

            _canceller.Cancel();
            _fileSystemWatcher.Stop();
        }

        public async Task DisconnectAsync()
        {
            IsConnected = false;

            _jsonProtocolMessenger.StopExecuting();
            await _tcpSocketClient.DisconnectAsync();

            _tcpSocketClient = new TcpSocketClient();
            ;
        }
    }
}