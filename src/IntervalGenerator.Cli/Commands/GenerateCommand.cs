using System.CommandLine;
using System.CommandLine.Invocation;
using IntervalGenerator.Core.Models;
using IntervalGenerator.Core.Services;
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
            description: "Output format (csv, json, console)",
            getDefaultValue: () => "console")
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
            var quiet = context.ParseResult.GetValueForOption(quietOption);

            var exitCode = await ExecuteAsync(
                startDate, endDate, period, profile, meters,
                deterministic, seed, output, format, quiet,
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
                Seed = seed
            };

            // Calculate expected readings
            var expectedCount = MultiMeterOrchestrator.CalculateExpectedReadingCount(config);

            if (!quiet)
            {
                Console.WriteLine("Interval Generator");
                Console.WriteLine("==================");
                Console.WriteLine($"Profile:      {profileName}");
                Console.WriteLine($"Date Range:   {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
                Console.WriteLine($"Period:       {periodMinutes} minutes");
                Console.WriteLine($"Meters:       {meterCount}");
                Console.WriteLine($"Deterministic: {(deterministic ? $"Yes (seed: {seed ?? config.GetHashCode()})" : "No")}");
                Console.WriteLine($"Expected:     {expectedCount:N0} readings");
                Console.WriteLine();
            }

            // Create orchestrator
            var orchestrator = new MultiMeterOrchestrator(registry.GetProfile);

            // Generate based on format
            if (format.Equals("console", StringComparison.OrdinalIgnoreCase) && outputFile == null)
            {
                await GenerateToConsoleAsync(orchestrator, config, quiet, cancellationToken);
            }
            else if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                await GenerateToCsvAsync(orchestrator, config, outputFile, quiet, cancellationToken);
            }
            else if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                // JSON output will be implemented with output formatters
                Console.Error.WriteLine("JSON output format not yet implemented. Use 'csv' or 'console'.");
                return 1;
            }
            else
            {
                await GenerateToConsoleAsync(orchestrator, config, quiet, cancellationToken);
            }

            if (!quiet)
            {
                Console.WriteLine();
                Console.WriteLine("Generation complete.");
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

    private static Task GenerateToConsoleAsync(
        MultiMeterOrchestrator orchestrator,
        GenerationConfiguration config,
        bool quiet,
        CancellationToken cancellationToken)
    {
        if (!quiet)
        {
            Console.WriteLine("MPAN,Timestamp,Period,ConsumptionKwh,MeasurementClass,QualityFlag");
        }
        else
        {
            Console.WriteLine("MPAN,Timestamp,Period,ConsumptionKwh,MeasurementClass,QualityFlag");
        }

        long count = 0;
        foreach (var reading in orchestrator.GenerateStreaming(config))
        {
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"{reading.Mpan},{reading.Timestamp:yyyy-MM-dd HH:mm:ss},{reading.Period},{reading.ConsumptionKwh},{reading.MeasurementClass},{reading.QualityFlag}");

            count++;
            if (!quiet && count % 10000 == 0)
            {
                Console.Error.WriteLine($"Generated {count:N0} readings...");
            }
        }

        return Task.CompletedTask;
    }

    private static async Task GenerateToCsvAsync(
        MultiMeterOrchestrator orchestrator,
        GenerationConfiguration config,
        FileInfo? outputFile,
        bool quiet,
        CancellationToken cancellationToken)
    {
        var filePath = outputFile?.FullName ?? $"intervals_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        if (!quiet)
        {
            Console.WriteLine($"Writing to: {filePath}");
        }

        await using var writer = new StreamWriter(filePath);
        await writer.WriteLineAsync("MPAN,Timestamp,Period,ConsumptionKwh,MeasurementClass,QualityFlag,BusinessType");

        long count = 0;
        foreach (var reading in orchestrator.GenerateStreaming(config))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await writer.WriteLineAsync(
                $"{reading.Mpan},{reading.Timestamp:yyyy-MM-dd HH:mm:ss},{reading.Period},{reading.ConsumptionKwh},{reading.MeasurementClass},{reading.QualityFlag},{reading.BusinessType}");

            count++;
            if (!quiet && count % 10000 == 0)
            {
                Console.Error.WriteLine($"Written {count:N0} readings...");
            }
        }

        if (!quiet)
        {
            Console.WriteLine($"Wrote {count:N0} readings to {filePath}");
        }
    }
}
