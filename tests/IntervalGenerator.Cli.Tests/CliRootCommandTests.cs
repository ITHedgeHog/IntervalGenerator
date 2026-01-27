using System.CommandLine;
using FluentAssertions;
using IntervalGenerator.Cli.Commands;

// CA1861: Prefer 'static readonly' fields over constant array arguments
// Suppressed for test readability - inline arrays in test assertions improve clarity and are acceptable
#pragma warning disable CA1861

namespace IntervalGenerator.Cli.Tests;

public class CliRootCommandTests
{
    private readonly RootCommand _rootCommand;

    public CliRootCommandTests()
    {
        _rootCommand = new RootCommand("Smart Meter Interval Generator - Generate realistic energy consumption data");
        _rootCommand.Subcommands.Add(GenerateCommand.Create());
    }

    [Fact]
    public void RootCommand_HasGenerateSubcommand()
    {
        // Assert
        var generateCommand = _rootCommand.Subcommands.FirstOrDefault(c => c.Name == "generate");
        generateCommand.Should().NotBeNull();
    }

    [Fact]
    public void RootCommand_HasCorrectDescription()
    {
        // Assert
        _rootCommand.Description.Should().Contain("Generate realistic energy consumption data");
    }

    [Fact]
    public void RootCommand_WithHelpOption_ShowsHelp()
    {
        // Act
        var result = _rootCommand.Parse(new[] { "--help" }).Invoke();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void RootCommand_WithGenerateHelp_ShowsGenerateOptions()
    {
        // Act
        var result = _rootCommand.Parse(new[] { "generate", "--help" }).Invoke();

        // Assert
        result.Should().Be(0);
    }
}
