namespace IntervalGenerator.Core.Randomization;

/// <summary>
/// Provides random number generation with support for both deterministic and non-deterministic modes.
/// </summary>
public interface IRandomNumberGenerator
{
    /// <summary>
    /// Gets the next random double between 0.0 (inclusive) and 1.0 (exclusive).
    /// </summary>
    /// <returns>A random double value.</returns>
    double NextDouble();

    /// <summary>
    /// Gets the next random integer between 0 (inclusive) and max (exclusive).
    /// </summary>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>A random integer value.</returns>
    int Next(int max);

    /// <summary>
    /// Gets the next random integer between min (inclusive) and max (exclusive).
    /// </summary>
    /// <param name="min">The inclusive lower bound.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <returns>A random integer value.</returns>
    int Next(int min, int max);

    /// <summary>
    /// Gets whether this generator is deterministic (seeded).
    /// </summary>
    bool IsDeterministic { get; }

    /// <summary>
    /// Gets the seed used for deterministic generation, or null if non-deterministic.
    /// </summary>
    int? Seed { get; }
}
