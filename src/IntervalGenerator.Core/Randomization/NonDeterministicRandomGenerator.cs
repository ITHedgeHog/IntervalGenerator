namespace IntervalGenerator.Core.Randomization;

/// <summary>
/// A non-deterministic random number generator that produces unpredictable sequences.
/// Uses the shared Random instance for thread-safe random number generation.
/// </summary>
public sealed class NonDeterministicRandomGenerator : IRandomNumberGenerator
{
    /// <inheritdoc />
    public bool IsDeterministic => false;

    /// <inheritdoc />
    public int? Seed => null;

    /// <inheritdoc />
    public double NextDouble() => Random.Shared.NextDouble();

    /// <inheritdoc />
    public int Next(int max)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(max);
        return Random.Shared.Next(max);
    }

    /// <inheritdoc />
    public int Next(int min, int max)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(min, max);
        return Random.Shared.Next(min, max);
    }
}
