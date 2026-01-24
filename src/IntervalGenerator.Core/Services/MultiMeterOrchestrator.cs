using IntervalGenerator.Core.Models;
using IntervalGenerator.Core.Profiles;
using IntervalGenerator.Core.Randomization;
using IntervalGenerator.Core.Utilities;

namespace IntervalGenerator.Core.Services;

/// <summary>
/// Orchestrates interval generation across multiple meters.
/// </summary>
public class MultiMeterOrchestrator
{
    private readonly Func<string, IConsumptionProfile> _profileResolver;

    /// <summary>
    /// Initializes a new instance of the MultiMeterOrchestrator.
    /// </summary>
    /// <param name="profileResolver">Function to resolve profiles by business type.</param>
    public MultiMeterOrchestrator(Func<string, IConsumptionProfile> profileResolver)
    {
        _profileResolver = profileResolver ?? throw new ArgumentNullException(nameof(profileResolver));
    }

    /// <summary>
    /// Generates interval readings for all meters according to the configuration.
    /// </summary>
    /// <param name="configuration">The generation configuration.</param>
    /// <returns>A GenerationResult containing all generated readings.</returns>
    public GenerationResult Generate(GenerationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ValidateConfiguration(configuration);

        var profile = _profileResolver(configuration.BusinessType);
        var randomGenerator = RandomGeneratorFactory.Create(configuration);
        var meterIds = GetOrGenerateMeterIds(configuration, randomGenerator);
        var mpanMap = MpanGenerator.GenerateMpans(meterIds);

        var allReadings = new List<IntervalReading>();

        foreach (var meterId in meterIds)
        {
            // For deterministic mode with multiple meters, create a per-meter seeded generator
            // to ensure each meter's sequence is reproducible
            var meterRandomGenerator = configuration.Deterministic
                ? CreateMeterSpecificGenerator(configuration, meterId)
                : randomGenerator;

            var engine = new IntervalGeneratorEngine(profile, meterRandomGenerator);

            var readings = engine.GenerateReadings(
                meterId,
                mpanMap[meterId],
                configuration.StartDate,
                configuration.EndDate,
                configuration.Period,
                configuration.MeasurementClass);

            allReadings.AddRange(readings);
        }

        return new GenerationResult
        {
            Readings = allReadings,
            Configuration = configuration
        };
    }

    /// <summary>
    /// Generates interval readings as a streaming enumerable for memory efficiency.
    /// Useful for large datasets where loading all readings into memory is impractical.
    /// </summary>
    /// <param name="configuration">The generation configuration.</param>
    /// <returns>An enumerable of interval readings.</returns>
    public IEnumerable<IntervalReading> GenerateStreaming(GenerationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ValidateConfiguration(configuration);

        var profile = _profileResolver(configuration.BusinessType);
        var randomGenerator = RandomGeneratorFactory.Create(configuration);
        var meterIds = GetOrGenerateMeterIds(configuration, randomGenerator);
        var mpanMap = MpanGenerator.GenerateMpans(meterIds);

        foreach (var meterId in meterIds)
        {
            var meterRandomGenerator = configuration.Deterministic
                ? CreateMeterSpecificGenerator(configuration, meterId)
                : randomGenerator;

            var engine = new IntervalGeneratorEngine(profile, meterRandomGenerator);

            foreach (var reading in engine.GenerateReadings(
                meterId,
                mpanMap[meterId],
                configuration.StartDate,
                configuration.EndDate,
                configuration.Period,
                configuration.MeasurementClass))
            {
                yield return reading;
            }
        }
    }

    /// <summary>
    /// Calculates the expected number of readings for the given configuration.
    /// </summary>
    /// <param name="configuration">The generation configuration.</param>
    /// <returns>The expected total number of readings.</returns>
    public static long CalculateExpectedReadingCount(GenerationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        int periodsPerDay = IntervalCalculator.GetPeriodsPerDay(configuration.Period);
        int totalDays = (configuration.EndDate.Date - configuration.StartDate.Date).Days + 1;

        return (long)totalDays * periodsPerDay * configuration.MeterCount;
    }

    private static void ValidateConfiguration(GenerationConfiguration configuration)
    {
        if (configuration.EndDate < configuration.StartDate)
        {
            throw new ArgumentException("End date must be greater than or equal to start date.");
        }

        if (configuration.MeterCount < 1 || configuration.MeterCount > 1000)
        {
            throw new ArgumentException("Meter count must be between 1 and 1000.");
        }

        if (string.IsNullOrWhiteSpace(configuration.BusinessType))
        {
            throw new ArgumentException("Business type is required.");
        }
    }

    private static List<Guid> GetOrGenerateMeterIds(
        GenerationConfiguration configuration,
        IRandomNumberGenerator randomGenerator)
    {
        if (configuration.MeterIds is { Count: > 0 })
        {
            // Use provided meter IDs
            return configuration.MeterIds.Take(configuration.MeterCount).ToList();
        }

        // Generate new meter IDs
        var meterIds = new List<Guid>(configuration.MeterCount);

        if (configuration.Deterministic && configuration.Seed.HasValue)
        {
            // For deterministic mode, generate reproducible GUIDs based on seed
            var guidRandom = new Random(configuration.Seed.Value);
            var buffer = new byte[16];

            for (int i = 0; i < configuration.MeterCount; i++)
            {
                guidRandom.NextBytes(buffer);
                meterIds.Add(new Guid(buffer));
            }
        }
        else
        {
            // Generate random GUIDs
            for (int i = 0; i < configuration.MeterCount; i++)
            {
                meterIds.Add(Guid.NewGuid());
            }
        }

        return meterIds;
    }

    private static IRandomNumberGenerator CreateMeterSpecificGenerator(
        GenerationConfiguration configuration,
        Guid meterId)
    {
        // Combine the configuration seed with the meter ID to get a unique but reproducible seed
        var combinedSeed = HashCode.Combine(
            configuration.Seed ?? configuration.GetHashCode(),
            meterId);

        return RandomGeneratorFactory.CreateDeterministic(combinedSeed);
    }
}
