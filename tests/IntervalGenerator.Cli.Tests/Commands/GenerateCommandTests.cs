using System.CommandLine;
using FluentAssertions;
using IntervalGenerator.Cli.Commands;

namespace IntervalGenerator.Cli.Tests.Commands;

public class GenerateCommandTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        // Act
        var command = GenerateCommand.Create();

        // Assert
        command.Name.Should().Be("generate");
        command.Description.Should().Contain("Generate interval consumption data");
    }

    [Fact]
    public void Create_HasRequiredOptions()
    {
        // Act
        var command = GenerateCommand.Create();

        // Assert
        command.Options.Should().NotBeEmpty();
        command.Options.Count.Should().BeGreaterThanOrEqualTo(12);
    }
}
