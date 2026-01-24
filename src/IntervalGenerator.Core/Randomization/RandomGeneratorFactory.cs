using IntervalGenerator.Core.Models;

namespace IntervalGenerator.Core.Randomization;

/// <summary>
/// Factory for creating random number generators based on generation configuration.
/// </summary>
public static class RandomGeneratorFactory
{
    /// <summary>
    /// Creates a random number generator based on the provided configuration.
    /// </summary>
    /// <param name="configuration">The generation configuration.</param>
    /// <returns>
    /// A <see cref="DeterministicRandomGenerator"/> if <see cref="GenerationConfiguration.Deterministic"/> is true,
    /// otherwise a <see cref="NonDeterministicRandomGenerator"/>.
    /// </returns>
    public static IRandomNumberGenerator Create(GenerationConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!configuration.Deterministic)
        {
            return new NonDeterministicRandomGenerator();
        }

        var seed = configuration.Seed ?? ComputeConfigurationHash(configuration);
        return new DeterministicRandomGenerator(seed);
    }

    /// <summary>
    /// Creates a deterministic random number generator with the specified seed.
    /// </summary>
    /// <param name="seed">The seed value.</param>
    /// <returns>A new <see cref="DeterministicRandomGenerator"/>.</returns>
    public static IRandomNumberGenerator CreateDeterministic(int seed)
    {
        return new DeterministicRandomGenerator(seed);
    }

    /// <summary>
    /// Creates a non-deterministic random number generator.
    /// </summary>
    /// <returns>A new <see cref="NonDeterministicRandomGenerator"/>.</returns>
    public static IRandomNumberGenerator CreateNonDeterministic()
    {
        return new NonDeterministicRandomGenerator();
    }

    /// <summary>
    /// Computes a deterministic hash from the configuration for use as a seed.
    /// This ensures the same configuration always produces the same data.
    /// </summary>
    private static int ComputeConfigurationHash(GenerationConfiguration configuration)
    {
        var hash = new HashCode();
        hash.Add(configuration.StartDate);
        hash.Add(configuration.EndDate);
        hash.Add(configuration.Period);
        hash.Add(configuration.BusinessType);
        hash.Add(configuration.MeasurementClass);
        hash.Add(configuration.MeterCount);
        hash.Add(configuration.SiteName);

        if (configuration.MeterIds is not null)
        {
            foreach (var id in configuration.MeterIds)
            {
                hash.Add(id);
            }
        }

        return hash.ToHashCode();
    }
}
