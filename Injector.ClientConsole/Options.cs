using CommandLine;
using CommandLine.Text;

namespace Injector.ClientConsole
{
    internal class Options
    {
        [Option('h', "host", DefaultValue = "localhost",
            HelpText = "Address of the listening injector host.")]
        public string HostAddress { get; set; }

        [Option('p', "port", DefaultValue = 51234,
            HelpText = "Port of the listening injector host.")]
        public int HostPort { get; set; }

        [Option('d', "directory", Required = true,
            HelpText = "Directory to watch for asssembly changes.")]
        public string WatchPath { get; set; }

        [Option('m', "filemask", DefaultValue = "*.dll",
            HelpText = "Filter for changed files.")]
        public string WatchMask { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
                current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}