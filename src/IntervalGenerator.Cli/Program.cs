using System.CommandLine;
using IntervalGenerator.Cli.Commands;

var rootCommand = new RootCommand("Smart Meter Interval Generator - Generate realistic energy consumption data");

rootCommand.Subcommands.Add(GenerateCommand.Create());

var parseResult = rootCommand.Parse(args);
return parseResult.Invoke();
