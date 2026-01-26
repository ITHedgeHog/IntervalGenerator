namespace IntervalGenerator.Core.Randomization;

/// <summary>
/// A seeded random number generator that produces reproducible sequences.
/// Given the same seed, this generator will always produce the same sequence of values.
/// </summary>
public sealed class DeterministicRandomGenerator : IRandomNumberGenerator
{
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance with the specified seed.
    /// </summary>
    /// <param name="seed">The seed value for reproducible random generation.</param>
    public DeterministicRandomGenerator(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    /// <inheritdoc />
    public bool IsDeterministic => true;

    /// <inheritdoc />
    public int? Seed { get; }

    /// <inheritdoc />
    public double NextDouble() => _random.NextDouble();

    /// <inheritdoc />
    public int Next(int max)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(max);
        return _random.Next(max);
    }

    /// <inheritdoc />
    public int Next(int min, int max)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(min, max);
        return _random.Next(min, max);
    }
}
