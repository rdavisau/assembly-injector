using System;
using System.Threading.Tasks;
using CommandLine;
using Injector.InjectorClient;
using Splat;

namespace Injector.ClientConsole
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Locator.CurrentMutable.RegisterConstant(new ConsoleLogger(), typeof (ILogger));

            var options = new Options();
            if (!Parser.Default.ParseArguments(args, options) || String.IsNullOrEmpty(options.WatchPath))
            {
                Console.WriteLine(options.GetUsage());
                return -1;
            }

            var host = options.HostAddress;
            var port = options.HostPort;
            var path = options.WatchPath;
            var mask = options.WatchMask;

            Console.WriteLine("Starting filesystem watcher..");
            var client = new FilesystemInjectorClient {WatchPath = path, WatchMask = mask};
            client.StartWatching();

            Console.Write("Attemping to connect.. ");
            Task.Run(() => client.ConnectAsync(host, port)).Wait();

            Console.WriteLine("Connected!\r\n\r\n");
            Console.WriteLine("Host: {0}:{1}", host, port);
            Console.WriteLine("Watching: {0} for {1}", path, mask);
            Console.WriteLine();

            Console.WriteLine("Press any key to disconnect.");

            Console.ReadKey();

            client.StopWatching();
            Task.Run(() => client.DisconnectAsync()).Wait();

            return 0;
        }
    }
}