using System.CommandLine;
using System.Globalization;
using FluentAssertions;
using IntervalGenerator.Cli.Commands;

namespace IntervalGenerator.Cli.Tests.Commands;

public class GenerateCommandIntegrationTests
{
    private readonly RootCommand _rootCommand;

    public GenerateCommandIntegrationTests()
    {
        _rootCommand = new RootCommand("Test CLI");
        _rootCommand.Subcommands.Add(GenerateCommand.Create());
    }

    #region Valid Input Tests

    [Fact]
    public void Generate_WithValidArgs_ReturnsSuccess()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-05",
                "--period", "30",
                "--profile", "Office",
                "--meters", "5",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            // Act
            var parseResult = _rootCommand.Parse(args);
            var result = parseResult.Invoke();

            // Assert
            result.Should().Be(0);
            File.Exists(tempFile).Should().BeTrue();
            var content = File.ReadAllText(tempFile);
            content.Should().NotBeEmpty();
            content.Should().Contain("MPAN");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Generate_WithShortOptions_ReturnsSuccess()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");

        try
        {
            var args = new[]
            {
                "generate",
                "-s", "2024-01-01",
                "-e", "2024-01-05",
                "-p", "15",
                "-t", "Office",
                "-m", "3",
                "-f", "csv",
                "-o", tempFile,
                "-q"
            };

            // Act
            var parseResult = _rootCommand.Parse(args);
            var result = parseResult.Invoke();

            // Assert
            result.Should().Be(0);
            File.Exists(tempFile).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Generate_WithJsonFormat_ReturnsValidJson()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-02",
                "--period", "30",
                "--profile", "Manufacturing",
                "--meters", "2",
                "--format", "json",
                "--output", tempFile,
                "--quiet"
            };

            // Act
            var parseResult = _rootCommand.Parse(args);
            var result = parseResult.Invoke();

            // Assert
            result.Should().Be(0);
            File.Exists(tempFile).Should().BeTrue();
            var content = File.ReadAllText(tempFile);
            // JSON output can be object or array depending on meter count
            var firstChar = content.TrimStart()[0];
            (firstChar == '{' || firstChar == '[').Should().BeTrue();
            // Validate it's valid JSON
            System.Text.Json.JsonDocument.Parse(content);

        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData("Office")]
    [InlineData("Manufacturing")]
    [InlineData("Retail")]
    [InlineData("DataCenter")]
    [InlineData("Educational")]
    public void Generate_WithValidProfiles_ReturnsSuccess(string profile)
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-02",
                "--period", "30",
                "--profile", profile,
                "--meters", "1",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            // Act
            var parseResult = _rootCommand.Parse(args);
            var result = parseResult.Invoke();

            // Assert
            result.Should().Be(0);
            File.Exists(tempFile).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(5)]
    [InlineData(15)]
    [InlineData(30)]
    public void Generate_WithValidPeriods_ReturnsSuccess(int period)
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-02",
                "--period", period.ToString(CultureInfo.InvariantCulture),
                "--profile", "Office",
                "--meters", "1",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            // Act
            var parseResult = _rootCommand.Parse(args);
            var result = parseResult.Invoke();

            // Assert
            result.Should().Be(0);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Generate_WithValidMeterCounts_ReturnsSuccess(int meterCount)
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-02",
                "--period", "30",
                "--profile", "Office",
                "--meters", meterCount.ToString(CultureInfo.InvariantCulture),
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            // Act
            var parseResult = _rootCommand.Parse(args);
            var result = parseResult.Invoke();

            // Assert
            result.Should().Be(0);
            var content = File.ReadAllText(tempFile);
            var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            // Header + data lines
            lines.Length.Should().BeGreaterThan(1);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Generate_WithDeterministicMode_ProducesSameOutput()
    {
        // Arrange
        var tempFile1 = Path.Combine(Path.GetTempPath(), $"test1_{Guid.NewGuid()}.csv");
        var tempFile2 = Path.Combine(Path.GetTempPath(), $"test2_{Guid.NewGuid()}.csv");

        try
        {
            var args1 = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-02",
                "--period", "30",
                "--profile", "Office",
                "--meters", "5",
                "--deterministic",
                "--seed", "42",
                "--format", "csv",
                "--output", tempFile1,
                "--quiet"
            };

            var args2 = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-02",
                "--period", "30",
                "--profile", "Office",
                "--meters", "5",
                "--deterministic",
                "--seed", "42",
                "--format", "csv",
                "--output", tempFile2,
                "--quiet"
            };

            // Act
            var result1 = _rootCommand.Parse(args1).Invoke();
            var result2 = _rootCommand.Parse(args2).Invoke();

            // Assert
            result1.Should().Be(0);
            result2.Should().Be(0);
            var content1 = File.ReadAllText(tempFile1);
            var content2 = File.ReadAllText(tempFile2);
            content1.Should().Be(content2);
        }
        finally
        {
            if (File.Exists(tempFile1))
                File.Delete(tempFile1);
            if (File.Exists(tempFile2))
                File.Delete(tempFile2);
        }
    }

    [Fact]
    public void Generate_WithSiteName_IncludesInOutput()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-02",
                "--period", "30",
                "--profile", "Office",
                "--meters", "2",
                "--format", "csv",
                "--output", tempFile,
                "--site", "Test Site",
                "--quiet"
            };

            // Act
            var parseResult = _rootCommand.Parse(args);
            var result = parseResult.Invoke();

            // Assert
            result.Should().Be(0);
            var content = File.ReadAllText(tempFile);
            content.Should().Contain("Test Site");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion

    #region Invalid Input Tests

    [Fact]
    public void Generate_WithEndDateBeforeStartDate_ReturnsFail()
    {
        // Arrange
        var args = new[]
        {
            "generate",
            "--start-date", "2024-01-10",
            "--end-date", "2024-01-01",
            "--period", "30",
            "--profile", "Office",
            "--meters", "1",
            "--quiet"
        };

        // Act
        var result = _rootCommand.Parse(args).Invoke();

        // Assert
        result.Should().NotBe(0);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(45)]
    public void Generate_WithInvalidPeriod_ReturnsFail(int invalidPeriod)
    {
        // Arrange
        var args = new[]
        {
            "generate",
            "--start-date", "2024-01-01",
            "--end-date", "2024-01-02",
            "--period", invalidPeriod.ToString(CultureInfo.InvariantCulture),
            "--profile", "Office",
            "--meters", "1",
            "--quiet"
        };

        // Act
        var result = _rootCommand.Parse(args).Invoke();

        // Assert
        result.Should().NotBe(0);
    }

    [Fact]
    public void Generate_WithInvalidProfile_ReturnsFail()
    {
        // Arrange
        var args = new[]
        {
            "generate",
            "--start-date", "2024-01-01",
            "--end-date", "2024-01-02",
            "--period", "30",
            "--profile", "InvalidProfile",
            "--meters", "1",
            "--quiet"
        };

        // Act
        var result = _rootCommand.Parse(args).Invoke();

        // Assert
        result.Should().NotBe(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Generate_WithInvalidMeterCount_ReturnsFail(int meterCount)
    {
        // Arrange
        var args = new[]
        {
            "generate",
            "--start-date", "2024-01-01",
            "--end-date", "2024-01-02",
            "--period", "30",
            "--profile", "Office",
            "--meters", meterCount.ToString(CultureInfo.InvariantCulture),
            "--quiet"
        };

        // Act
        var result = _rootCommand.Parse(args).Invoke();

        // Assert
        result.Should().NotBe(0);
    }

    [Fact]
    public void Generate_WithInvalidFormat_ReturnsFail()
    {
        // Arrange
        var args = new[]
        {
            "generate",
            "--start-date", "2024-01-01",
            "--end-date", "2024-01-02",
            "--period", "30",
            "--profile", "Office",
            "--meters", "1",
            "--format", "xml",
            "--quiet"
        };

        // Act
        var result = _rootCommand.Parse(args).Invoke();

        // Assert
        result.Should().NotBe(0);
    }

    [Fact]
    public void Generate_WithBadlyFormattedDateFormat_StillWorks()
    {
        // Arrange - System.CommandLine DateTime parser is lenient
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "01-01-2024",
                "--end-date", "2024-01-02",
                "--period", "30",
                "--profile", "Office",
                "--meters", "1",
                "--quiet",
                "--output", tempFile
            };

            // Act
            var result = _rootCommand.Parse(args).Invoke();

            // Assert - Should succeed since parser accepts both formats
            result.Should().Be(0);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion

    #region Output Validation Tests

    [Fact]
    public void Generate_CsvOutput_HasCorrectStructure()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-02",
                "--period", "30",
                "--profile", "Office",
                "--meters", "2",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            // Act
            _rootCommand.Parse(args).Invoke();

            // Assert
            var lines = File.ReadAllLines(tempFile);
            lines.Length.Should().BeGreaterThan(1);

            // Check header
            var header = lines[0];
            var expectedColumns = new[] { "MPAN", "Site", "MeasurementClass", "Date", "Period", "HHC", "AEI", "QtyId" };
            foreach (var column in expectedColumns)
            {
                header.Should().Contain(column);
            }

            // Check data lines have same column count
            var headerColumnCount = header.Split(',').Length;
            foreach (var line in lines.Skip(1))
            {
                line.Split(',').Length.Should().Be(headerColumnCount);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Generate_JsonOutput_IsValid()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-02",
                "--period", "30",
                "--profile", "Office",
                "--meters", "1",
                "--format", "json",
                "--output", tempFile,
                "--quiet"
            };

            // Act
            _rootCommand.Parse(args).Invoke();

            // Assert
            var content = File.ReadAllText(tempFile);
            content.Should().NotBeNullOrEmpty();

            // Try to parse as JSON (basic validation)
            var doc = System.Text.Json.JsonDocument.Parse(content);
            doc.RootElement.ValueKind.Should().BeOneOf(
                System.Text.Json.JsonValueKind.Object,
                System.Text.Json.JsonValueKind.Array);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Generate_ConsoleOutput_WithoutOutputFile()
    {
        // Arrange
        var args = new[]
        {
            "generate",
            "--start-date", "2024-01-01",
            "--end-date", "2024-01-02",
            "--period", "30",
            "--profile", "Office",
            "--meters", "1",
            "--format", "csv",
            "--quiet"
        };

        // Act & Assert - Should complete without error
        var result = _rootCommand.Parse(args).Invoke();
        result.Should().Be(0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Generate_WithLongDateRange_Succeeds()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-12-31",
                "--period", "30",
                "--profile", "Office",
                "--meters", "1",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            // Act
            var result = _rootCommand.Parse(args).Invoke();

            // Assert
            result.Should().Be(0);
            File.Exists(tempFile).Should().BeTrue();
            var fileSize = new FileInfo(tempFile).Length;
            fileSize.Should().BeGreaterThan(0);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Generate_WithSingleDay_Succeeds()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-01",
                "--period", "30",
                "--profile", "Office",
                "--meters", "1",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            // Act
            var result = _rootCommand.Parse(args).Invoke();

            // Assert
            result.Should().Be(0);
            var content = File.ReadAllText(tempFile);
            content.Should().Contain("2024-01-01");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Generate_WithMaxMeterCount_Succeeds()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-02",
                "--period", "30",
                "--profile", "Office",
                "--meters", "1000",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            // Act
            var result = _rootCommand.Parse(args).Invoke();

            // Assert
            result.Should().Be(0);
            File.Exists(tempFile).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion
}
