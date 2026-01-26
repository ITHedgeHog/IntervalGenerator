using FluentAssertions;
using IntervalGenerator.Core.Randomization;

namespace IntervalGenerator.Core.Tests.Randomization;

public class DeterministicRandomGeneratorTests
{
    [Fact]
    public void IsDeterministic_ReturnsTrue()
    {
        var generator = new DeterministicRandomGenerator(42);

        generator.IsDeterministic.Should().BeTrue();
    }

    [Fact]
    public void Seed_ReturnsProvidedSeed()
    {
        const int seed = 12345;
        var generator = new DeterministicRandomGenerator(seed);

        generator.Seed.Should().Be(seed);
    }

    [Fact]
    public void NextDouble_SameSeed_ProducesSameSequence()
    {
        const int seed = 42;
        var generator1 = new DeterministicRandomGenerator(seed);
        var generator2 = new DeterministicRandomGenerator(seed);

        var sequence1 = Enumerable.Range(0, 100).Select(_ => generator1.NextDouble()).ToList();
        var sequence2 = Enumerable.Range(0, 100).Select(_ => generator2.NextDouble()).ToList();

        sequence1.Should().BeEquivalentTo(sequence2, options => options.WithStrictOrdering());
    }

    [Fact]
    public void NextDouble_DifferentSeeds_ProduceDifferentSequences()
    {
        var generator1 = new DeterministicRandomGenerator(42);
        var generator2 = new DeterministicRandomGenerator(43);

        var sequence1 = Enumerable.Range(0, 10).Select(_ => generator1.NextDouble()).ToList();
        var sequence2 = Enumerable.Range(0, 10).Select(_ => generator2.NextDouble()).ToList();

        sequence1.Should().NotBeEquivalentTo(sequence2);
    }

    [Fact]
    public void NextDouble_ReturnsValuesBetween0And1()
    {
        var generator = new DeterministicRandomGenerator(42);

        var values = Enumerable.Range(0, 1000).Select(_ => generator.NextDouble()).ToList();

        values.Should().AllSatisfy(v =>
        {
            v.Should().BeGreaterThanOrEqualTo(0.0);
            v.Should().BeLessThan(1.0);
        });
    }

    [Fact]
    public void Next_WithMax_ReturnsValueInRange()
    {
        var generator = new DeterministicRandomGenerator(42);
        const int max = 100;

        var values = Enumerable.Range(0, 1000).Select(_ => generator.NextInt(max)).ToList();

        values.Should().AllSatisfy(v =>
        {
            v.Should().BeGreaterThanOrEqualTo(0);
            v.Should().BeLessThan(max);
        });
    }

    [Fact]
    public void Next_WithMax_SameSeed_ProducesSameSequence()
    {
        const int seed = 42;
        var generator1 = new DeterministicRandomGenerator(seed);
        var generator2 = new DeterministicRandomGenerator(seed);

        var sequence1 = Enumerable.Range(0, 50).Select(_ => generator1.NextInt(100)).ToList();
        var sequence2 = Enumerable.Range(0, 50).Select(_ => generator2.NextInt(100)).ToList();

        sequence1.Should().BeEquivalentTo(sequence2, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Next_WithMinMax_ReturnsValueInRange()
    {
        var generator = new DeterministicRandomGenerator(42);
        const int min = 50;
        const int max = 100;

        var values = Enumerable.Range(0, 1000).Select(_ => generator.NextInt(min, max)).ToList();

        values.Should().AllSatisfy(v =>
        {
            v.Should().BeGreaterThanOrEqualTo(min);
            v.Should().BeLessThan(max);
        });
    }

    [Fact]
    public void Next_ZeroOrNegativeMax_ThrowsArgumentOutOfRangeException()
    {
        var generator = new DeterministicRandomGenerator(42);

        var actZero = () => generator.NextInt(0);
        var actNegative = () => generator.NextInt(-1);

        actZero.Should().Throw<ArgumentOutOfRangeException>();
        actNegative.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Next_MinGreaterThanOrEqualMax_ThrowsArgumentOutOfRangeException()
    {
        var generator = new DeterministicRandomGenerator(42);

        var actEqual = () => generator.NextInt(10, 10);
        var actGreater = () => generator.NextInt(20, 10);

        actEqual.Should().Throw<ArgumentOutOfRangeException>();
        actGreater.Should().Throw<ArgumentOutOfRangeException>();
    }
}
