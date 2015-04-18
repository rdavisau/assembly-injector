using System;
using Splat;

namespace Injector.ClientConsole
{
    internal class ConsoleLogger : ILogger
    {
        public LogLevel Level { get; set; }

        public void Write(string message, LogLevel logLevel)
        {
            Console.WriteLine(message);
        }
    }
}