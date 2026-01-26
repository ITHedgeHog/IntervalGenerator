using System.CommandLine;
using System.CommandLine.Invocation;
using IntervalGenerator.Core.Models;
using IntervalGenerator.Core.Services;
using IntervalGenerator.Output;
using IntervalGenerator.Profiles;

namespace IntervalGenerator.Cli.Commands;

/// <summary>
/// The generate command for creating interval consumption data.
/// </summary>
public static class GenerateCommand
{
    public static Command Create()
    {
        var startDateOption = new Option<DateTime>(
            aliases: ["--start-date", "-s"],
            getDefaultValue: () => DateTime.Today.AddDays(-7))
        {
            Name = "start-date",
            Description = "Start date for generation (inclusive)"
        };

        var endDateOption = new Option<DateTime>(
            aliases: ["--end-date", "-e"],
            getDefaultValue: () => DateTime.Today)
        {
            Name = "end-date",
            Description = "End date for generation (inclusive)"
        };

        var periodOption = new Option<int>(
            aliases: ["--period", "-p"],
            getDefaultValue: () => 30)
        {
            Name = "period",
            Description = "Interval period in minutes (5, 15, or 30)"
        };

        var profileOption = new Option<string>(
            aliases: ["--profile", "-t"],
            getDefaultValue: () => "Office")
        {
            Name = "profile",
            Description = "Business type profile (Office, Manufacturing, Retail, DataCenter, Educational)"
        };

        var metersOption = new Option<int>(
            aliases: ["--meters", "-m"],
            getDefaultValue: () => 1)
        {
            Name = "meters",
            Description = "Number of meters to generate (1-1000)"
        };

        var deterministicOption = new Option<bool>(
            aliases: ["--deterministic", "-d"],
            getDefaultValue: () => false)
        {
            Name = "deterministic",
            Description = "Enable deterministic mode for reproducible output"
        };

        var seedOption = new Option<int?>(
            aliases: ["--seed"],
            getDefaultValue: () => null)
        {
            Name = "seed",
            Description = "Random seed for deterministic mode"
        };

        var outputOption = new Option<FileInfo?>(
            aliases: ["--output", "-o"],
            getDefaultValue: () => null)
        {
            Name = "output",
            Description = "Output file path (outputs to console if not specified)"
        };

        var formatOption = new Option<string>(
            aliases: ["--format", "-f"],
            getDefaultValue: () => "csv")
        {
            Name = "format",
            Description = "Output format (csv, json)"
        };

        var siteNameOption = new Option<string?>(
            aliases: ["--site"],
            getDefaultValue: () => null)
        {
            Name = "site",
            Description = "Site name to include in output"
        };

        var prettyOption = new Option<bool>(
            aliases: ["--pretty"],
            getDefaultValue: () => false)
        {
            Name = "pretty",
            Description = "Pretty-print JSON output"
        };

        var quietOption = new Option<bool>(
            aliases: ["--quiet", "-q"],
            getDefaultValue: () => false)
        {
            Name = "quiet",
            Description = "Suppress progress output"
        };

        var command = new Command("generate", "Generate interval consumption data")
        {
            startDateOption,
            endDateOption,
            periodOption,
            profileOption,
            metersOption,
            deterministicOption,
            seedOption,
            outputOption,
            formatOption,
            siteNameOption,
            prettyOption,
            quietOption
        };

        command.Handler = CommandHandler.Create(
            (DateTime startDate, DateTime endDate, int period, string profile, int meters,
             bool deterministic, int? seed, FileInfo? output, string format, string? site,
             bool pretty, bool quiet) =>
        {
            return ExecuteAsync(
                startDate, endDate, period, profile, meters,
                deterministic, seed, output, format, site, pretty, quiet,
                CancellationToken.None).Result;
        });

        return command;
    }

    private static async Task<int> ExecuteAsync(
        DateTime startDate,
        DateTime endDate,
        int periodMinutes,
        string profileName,
        int meterCount,
        bool deterministic,
        int? seed,
        FileInfo? outputFile,
        string format,
        string? siteName,
        bool prettyPrint,
        bool quiet,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate dates
            if (endDate < startDate)
            {
                Console.Error.WriteLine("Error: End date must be greater than or equal to start date.");
                return 1;
            }

            // Validate period
            if (periodMinutes != 5 && periodMinutes != 15 && periodMinutes != 30)
            {
                Console.Error.WriteLine("Error: Period must be 5, 15, or 30 minutes.");
                return 1;
            }

            // Validate profile
            var registry = new ProfileRegistry();
            if (!registry.IsRegistered(profileName))
            {
                Console.Error.WriteLine($"Error: Unknown profile '{profileName}'.");
                Console.Error.WriteLine($"Available profiles: {string.Join(", ", registry.GetAvailableBusinessTypes())}");
                return 1;
            }

            // Validate meter count
            if (meterCount < 1 || meterCount > 1000)
            {
                Console.Error.WriteLine("Error: Meter count must be between 1 and 1000.");
                return 1;
            }

            // Validate format
            if (!OutputFormatterFactory.IsSupported(format))
            {
                Console.Error.WriteLine($"Error: Unsupported format '{format}'.");
                Console.Error.WriteLine($"Supported: {string.Join(", ", OutputFormatterFactory.GetSupportedFormats())}");
                return 1;
            }

            var intervalPeriod = periodMinutes == 5
                ? IntervalPeriod.FiveMinute
                : periodMinutes == 15
                    ? IntervalPeriod.FifteenMinute
                    : IntervalPeriod.ThirtyMinute;

            var config = new GenerationConfiguration
            {
                StartDate = startDate,
                EndDate = endDate,
                Period = intervalPeriod,
                BusinessType = profileName,
                MeterCount = meterCount,
                Deterministic = deterministic,
                Seed = seed,
                SiteName = siteName
            };

            // Calculate expected readings
            var expectedCount = MultiMeterOrchestrator.CalculateExpectedReadingCount(config);

            if (!quiet)
            {
                Console.Error.WriteLine("Interval Generator");
                Console.Error.WriteLine("==================");
                Console.Error.WriteLine($"Profile:       {profileName}");
                Console.Error.WriteLine($"Date Range:    {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                Console.Error.WriteLine($"Period:        {periodMinutes} minutes");
                Console.Error.WriteLine($"Meters:        {meterCount}");
                Console.Error.WriteLine($"Deterministic: {(deterministic ? $"Yes (seed: {seed ?? config.GetHashCode()})" : "No")}");
                Console.Error.WriteLine($"Format:        {format}");
                Console.Error.WriteLine($"Expected:      {expectedCount:N0} readings");
                Console.Error.WriteLine();
            }

            // Create orchestrator and formatter
            var orchestrator = new MultiMeterOrchestrator(registry.GetProfile);
            var formatter = OutputFormatterFactory.Create(format);

            var outputOptions = new OutputOptions
            {
                SiteName = siteName ?? config.SiteName,
                IncludeHeaders = true,
                PrettyPrint = prettyPrint
            };

            // Generate readings
            var readings = orchestrator.GenerateStreaming(config);

            // For JSON format, we need to materialize the enumerable since the format requires grouping
            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                if (!quiet)
                {
                    Console.Error.WriteLine("Generating readings...");
                }
                readings = orchestrator.Generate(config).Readings;
            }

            // Output to file or console
            if (outputFile != null)
            {
                if (!quiet)
                {
                    Console.Error.WriteLine($"Writing to: {outputFile.FullName}");
                }
                await formatter.WriteToFileAsync(readings, outputFile.FullName, outputOptions, cancellationToken);
                if (!quiet)
                {
                    Console.Error.WriteLine($"Wrote {expectedCount:N0} readings to {outputFile.FullName}");
                }
            }
            else
            {
                // Output to stdout
                await using var stdout = Console.OpenStandardOutput();
                await formatter.WriteAsync(readings, stdout, outputOptions, cancellationToken);
            }

            if (!quiet)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Generation complete.");
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Operation cancelled.");
            return 130;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
