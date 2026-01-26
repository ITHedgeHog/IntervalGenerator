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
            description: "Start date for generation (inclusive)",
            getDefaultValue: () => DateTime.Today.AddDays(-7))
        {
            IsRequired = false
        };

        var endDateOption = new Option<DateTime>(
            aliases: ["--end-date", "-e"],
            description: "End date for generation (inclusive)",
            getDefaultValue: () => DateTime.Today)
        {
            IsRequired = false
        };

        var periodOption = new Option<int>(
            aliases: ["--period", "-p"],
            description: "Interval period in minutes (15 or 30)",
            getDefaultValue: () => 30)
        {
            IsRequired = false
        };
        periodOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(periodOption);
            if (value != 15 && value != 30)
            {
                result.ErrorMessage = "Period must be 15 or 30 minutes";
            }
        });

        var profileOption = new Option<string>(
            aliases: ["--profile", "-t"],
            description: "Business type profile (Office, Manufacturing, Retail, DataCenter, Educational)",
            getDefaultValue: () => "Office")
        {
            IsRequired = false
        };

        var metersOption = new Option<int>(
            aliases: ["--meters", "-m"],
            description: "Number of meters to generate (1-1000)",
            getDefaultValue: () => 1)
        {
            IsRequired = false
        };
        metersOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(metersOption);
            if (value < 1 || value > 1000)
            {
                result.ErrorMessage = "Meter count must be between 1 and 1000";
            }
        });

        var deterministicOption = new Option<bool>(
            aliases: ["--deterministic", "-d"],
            description: "Enable deterministic mode for reproducible output",
            getDefaultValue: () => false)
        {
            IsRequired = false
        };

        var seedOption = new Option<int?>(
            aliases: ["--seed"],
            description: "Random seed for deterministic mode")
        {
            IsRequired = false
        };

        var outputOption = new Option<FileInfo?>(
            aliases: ["--output", "-o"],
            description: "Output file path (outputs to console if not specified)")
        {
            IsRequired = false
        };

        var formatOption = new Option<string>(
            aliases: ["--format", "-f"],
            description: "Output format (csv, json)",
            getDefaultValue: () => "csv")
        {
            IsRequired = false
        };
        formatOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(formatOption);
            if (!string.IsNullOrEmpty(value) && !OutputFormatterFactory.IsSupported(value))
            {
                result.ErrorMessage = $"Unsupported format '{value}'. Supported: {string.Join(", ", OutputFormatterFactory.GetSupportedFormats())}";
            }
        });

        var siteNameOption = new Option<string?>(
            aliases: ["--site"],
            description: "Site name to include in output")
        {
            IsRequired = false
        };

        var prettyOption = new Option<bool>(
            aliases: ["--pretty"],
            description: "Pretty-print JSON output",
            getDefaultValue: () => false)
        {
            IsRequired = false
        };

        var quietOption = new Option<bool>(
            aliases: ["--quiet", "-q"],
            description: "Suppress progress output",
            getDefaultValue: () => false)
        {
            IsRequired = false
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

        command.SetHandler(async (InvocationContext context) =>
        {
            var startDate = context.ParseResult.GetValueForOption(startDateOption);
            var endDate = context.ParseResult.GetValueForOption(endDateOption);
            var period = context.ParseResult.GetValueForOption(periodOption);
            var profile = context.ParseResult.GetValueForOption(profileOption)!;
            var meters = context.ParseResult.GetValueForOption(metersOption);
            var deterministic = context.ParseResult.GetValueForOption(deterministicOption);
            var seed = context.ParseResult.GetValueForOption(seedOption);
            var output = context.ParseResult.GetValueForOption(outputOption);
            var format = context.ParseResult.GetValueForOption(formatOption)!;
            var siteName = context.ParseResult.GetValueForOption(siteNameOption);
            var pretty = context.ParseResult.GetValueForOption(prettyOption);
            var quiet = context.ParseResult.GetValueForOption(quietOption);

            var exitCode = await ExecuteAsync(
                startDate, endDate, period, profile, meters,
                deterministic, seed, output, format, siteName, pretty, quiet,
                context.GetCancellationToken());

            context.ExitCode = exitCode;
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

            // Validate profile
            var registry = new ProfileRegistry();
            if (!registry.IsRegistered(profileName))
            {
                Console.Error.WriteLine($"Error: Unknown profile '{profileName}'.");
                Console.Error.WriteLine($"Available profiles: {string.Join(", ", registry.GetAvailableBusinessTypes())}");
                return 1;
            }

            var intervalPeriod = periodMinutes == 15
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
