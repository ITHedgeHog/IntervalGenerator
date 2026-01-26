using FluentAssertions;
using IntervalGenerator.Core.Randomization;

namespace IntervalGenerator.Core.Tests.Randomization;

public class NonDeterministicRandomGeneratorTests
{
    [Fact]
    public void IsDeterministic_ReturnsFalse()
    {
        var generator = new NonDeterministicRandomGenerator();

        generator.IsDeterministic.Should().BeFalse();
    }

    [Fact]
    public void Seed_ReturnsNull()
    {
        var generator = new NonDeterministicRandomGenerator();

        generator.Seed.Should().BeNull();
    }

    [Fact]
    public void NextDouble_ReturnsValuesBetween0And1()
    {
        var generator = new NonDeterministicRandomGenerator();

        var values = Enumerable.Range(0, 1000).Select(_ => generator.NextDouble()).ToList();

        values.Should().AllSatisfy(v =>
        {
            v.Should().BeGreaterThanOrEqualTo(0.0);
            v.Should().BeLessThan(1.0);
        });
    }

    [Fact]
    public void NextDouble_ProducesVariedValues()
    {
        var generator = new NonDeterministicRandomGenerator();

        var values = Enumerable.Range(0, 100).Select(_ => generator.NextDouble()).ToList();

        // Should have variation (not all the same value)
        values.Distinct().Count().Should().BeGreaterThan(90);
    }

    [Fact]
    public void Next_WithMax_ReturnsValueInRange()
    {
        var generator = new NonDeterministicRandomGenerator();
        const int max = 100;

        var values = Enumerable.Range(0, 1000).Select(_ => generator.NextInt(max)).ToList();

        values.Should().AllSatisfy(v =>
        {
            v.Should().BeGreaterThanOrEqualTo(0);
            v.Should().BeLessThan(max);
        });
    }

    [Fact]
    public void Next_WithMinMax_ReturnsValueInRange()
    {
        var generator = new NonDeterministicRandomGenerator();
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
        var generator = new NonDeterministicRandomGenerator();

        var actZero = () => generator.NextInt(0);
        var actNegative = () => generator.NextInt(-1);

        actZero.Should().Throw<ArgumentOutOfRangeException>();
        actNegative.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Next_MinGreaterThanOrEqualMax_ThrowsArgumentOutOfRangeException()
    {
        var generator = new NonDeterministicRandomGenerator();

        var actEqual = () => generator.NextInt(10, 10);
        var actGreater = () => generator.NextInt(20, 10);

        actEqual.Should().Throw<ArgumentOutOfRangeException>();
        actGreater.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MultipleInstances_UseSharedRandom_ThreadSafe()
    {
        // This test verifies that multiple instances can be used concurrently
        var generators = Enumerable.Range(0, 10)
            .Select(_ => new NonDeterministicRandomGenerator())
            .ToList();

        var tasks = generators.Select(g =>
            Task.Run(() => Enumerable.Range(0, 100).Select(_ => g.NextDouble()).ToList())
        ).ToArray();

        var act = () => Task.WaitAll(tasks);

        act.Should().NotThrow();
    }
}
