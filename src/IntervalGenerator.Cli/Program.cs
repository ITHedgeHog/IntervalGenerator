using System.CommandLine;
using IntervalGenerator.Cli.Commands;

var rootCommand = new RootCommand("Smart Meter Interval Generator - Generate realistic energy consumption data")
{
    GenerateCommand.Create()
};

return rootCommand.Invoke(args);
