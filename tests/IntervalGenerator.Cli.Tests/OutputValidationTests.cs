using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using IntervalGenerator.Cli.Commands;

namespace IntervalGenerator.Cli.Tests;

public class OutputValidationTests
{
    private readonly RootCommand _rootCommand;

    public OutputValidationTests()
    {
        _rootCommand = new RootCommand("Test CLI");
        _rootCommand.Subcommands.Add(GenerateCommand.Create());
    }

    #region CSV Output Tests

    [Fact]
    public void CsvOutput_ContainsExpectedColumns()
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
                "--meters", "1",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            _rootCommand.Parse(args).Invoke();
            var lines = File.ReadAllLines(tempFile);

            // Assert - Check all required columns are present
            var header = lines[0];
            var columns = header.Split(',');

            columns.Should().Contain("MPAN");
            columns.Should().Contain("Site");
            columns.Should().Contain("MeasurementClass");
            columns.Should().Contain("Date");
            columns.Should().Contain("Period");
            columns.Should().Contain("HHC");
            columns.Should().Contain("AEI");
            columns.Should().Contain("QtyId");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CsvOutput_HasConsistentColumnCount()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.csv");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-03",
                "--meters", "3",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            _rootCommand.Parse(args).Invoke();
            var lines = File.ReadAllLines(tempFile);

            // Assert
            var headerColumnCount = lines[0].Split(',').Length;
            foreach (var line in lines.Skip(1))
            {
                line.Split(',').Length.Should().Be(headerColumnCount, $"Line should have {headerColumnCount} columns");
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CsvOutput_ContainsValidMpans()
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
                "--meters", "5",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            _rootCommand.Parse(args).Invoke();
            var lines = File.ReadAllLines(tempFile);

            // Assert - Check that MPANs are 13-digit numbers
            foreach (var line in lines.Skip(1))
            {
                var columns = line.Split(',');
                var mpan = columns[0];
                mpan.Should().HaveLength(13, "MPAN should be 13 digits");
                long.TryParse(mpan, out _).Should().BeTrue("MPAN should contain only digits");
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CsvOutput_ContainsValidDates()
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
                "--meters", "1",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            _rootCommand.Parse(args).Invoke();
            var lines = File.ReadAllLines(tempFile);

            // Assert - Check that dates are valid and within range
            foreach (var line in lines.Skip(1))
            {
                var columns = line.Split(',');
                var dateString = columns[3]; // Date column

                DateOnly.TryParse(dateString, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var date).Should().BeTrue($"'{dateString}' should be a valid date");

                var startDate = DateOnly.Parse("2024-01-01", CultureInfo.InvariantCulture);
                var endDate = DateOnly.Parse("2024-01-05", CultureInfo.InvariantCulture);
                date.CompareTo(startDate).Should().BeGreaterThanOrEqualTo(0);
                date.CompareTo(endDate).Should().BeLessThanOrEqualTo(0);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CsvOutput_ContainsValidPeriods()
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
                "--meters", "1",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            _rootCommand.Parse(args).Invoke();
            var lines = File.ReadAllLines(tempFile);

            // Assert - 30-minute period means 48 periods per day
            var periodCounts = new Dictionary<int, int>();
            foreach (var line in lines.Skip(1))
            {
                var columns = line.Split(',');
                var periodString = columns[4]; // Period column

                int.TryParse(periodString, out var period).Should().BeTrue();
                period.Should().BeGreaterThanOrEqualTo(1);
                period.Should().BeLessThanOrEqualTo(48);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CsvOutput_ContainsValidConsumptionValues()
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
                "--meters", "1",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            _rootCommand.Parse(args).Invoke();
            var lines = File.ReadAllLines(tempFile);

            // Assert - HHC (consumption) should be non-negative decimal
            foreach (var line in lines.Skip(1))
            {
                var columns = line.Split(',');
                var hhc = columns[5]; // HHC column

                decimal.TryParse(hhc, out var consumption).Should().BeTrue();
                consumption.Should().BeGreaterThanOrEqualTo(0);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void CsvOutput_ContainsValidQualityFlags()
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
                "--meters", "1",
                "--format", "csv",
                "--output", tempFile,
                "--quiet"
            };

            _rootCommand.Parse(args).Invoke();
            var lines = File.ReadAllLines(tempFile);

            // Assert - AEI (quality flag) should be A, E, M, or X
            var validFlags = new[] { "A", "E", "M", "X" };
            foreach (var line in lines.Skip(1))
            {
                var columns = line.Split(',');
                var aei = columns[6]; // AEI column

                validFlags.Should().Contain(aei);
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion

    #region JSON Output Tests

    [Fact]
    public void JsonOutput_IsValidJsonArray()
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
                "--meters", "1",
                "--format", "json",
                "--output", tempFile,
                "--quiet"
            };

            _rootCommand.Parse(args).Invoke();
            var content = File.ReadAllText(tempFile);

            // Act & Assert
            var doc = JsonDocument.Parse(content);
            doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void JsonOutput_ContainsExpectedFields()
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
                "--meters", "1",
                "--format", "json",
                "--output", tempFile,
                "--quiet"
            };

            _rootCommand.Parse(args).Invoke();
            var content = File.ReadAllText(tempFile);
            var doc = JsonDocument.Parse(content);

            // Assert - Check object has expected properties (Electralink format)
            doc.RootElement.TryGetProperty("MPAN", out _).Should().BeTrue();
            doc.RootElement.TryGetProperty("MC", out _).Should().BeTrue();

            // Verify period keys are P-prefixed
            var mc = doc.RootElement.GetProperty("MC");
            var ai = mc.GetProperty("AI");
            var firstDate = ai.EnumerateObject().First();
            firstDate.Value.TryGetProperty("P1", out _).Should().BeTrue("Period keys should be P-prefixed");
            firstDate.Value.TryGetProperty("P49", out _).Should().BeTrue("P49 should be present");
            firstDate.Value.TryGetProperty("P50", out _).Should().BeTrue("P50 should be present");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void JsonOutput_WithPrettyFlag_IsFormatted()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.json");

        try
        {
            var args = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-01",
                "--meters", "1",
                "--format", "json",
                "--output", tempFile,
                "--pretty",
                "--quiet"
            };

            _rootCommand.Parse(args).Invoke();
            var content = File.ReadAllText(tempFile);

            // Assert - Pretty-printed JSON should have indentation
            content.Should().Contain("\n");
            content.Should().Contain("  ");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    #endregion

    #region Data Integrity Tests

    [Fact]
    public void Output_WithDeterministicSeed_IsDeterministic()
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
                "--meters", "5",
                "--deterministic",
                "--seed", "12345",
                "--format", "csv",
                "--output", tempFile1,
                "--quiet"
            };

            var args2 = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-02",
                "--meters", "5",
                "--deterministic",
                "--seed", "12345",
                "--format", "csv",
                "--output", tempFile2,
                "--quiet"
            };

            // Act
            _rootCommand.Parse(args1).Invoke();
            _rootCommand.Parse(args2).Invoke();

            // Assert
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
    public void Output_WithDifferentSeeds_IsDifferent()
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
                "--meters", "5",
                "--deterministic",
                "--seed", "111",
                "--format", "csv",
                "--output", tempFile1,
                "--quiet"
            };

            var args2 = new[]
            {
                "generate",
                "--start-date", "2024-01-01",
                "--end-date", "2024-01-02",
                "--meters", "5",
                "--deterministic",
                "--seed", "222",
                "--format", "csv",
                "--output", tempFile2,
                "--quiet"
            };

            // Act
            _rootCommand.Parse(args1).Invoke();
            _rootCommand.Parse(args2).Invoke();

            // Assert
            var content1 = File.ReadAllText(tempFile1);
            var content2 = File.ReadAllText(tempFile2);
            content1.Should().NotBe(content2);
        }
        finally
        {
            if (File.Exists(tempFile1))
                File.Delete(tempFile1);
            if (File.Exists(tempFile2))
                File.Delete(tempFile2);
        }
    }

    #endregion
}
