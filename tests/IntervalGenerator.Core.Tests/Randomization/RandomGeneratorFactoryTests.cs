using FluentAssertions;
using IntervalGenerator.Core.Models;
using IntervalGenerator.Core.Randomization;

namespace IntervalGenerator.Core.Tests.Randomization;

public class RandomGeneratorFactoryTests
{
    #region Create from Configuration Tests

    [Fact]
    public void Create_NonDeterministicConfig_ReturnsNonDeterministicGenerator()
    {
        var config = new GenerationConfiguration
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today,
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            Deterministic = false
        };

        var generator = RandomGeneratorFactory.Create(config);

        generator.Should().BeOfType<NonDeterministicRandomGenerator>();
        generator.IsDeterministic.Should().BeFalse();
    }

    [Fact]
    public void Create_DeterministicConfigWithSeed_ReturnsDeterministicGenerator()
    {
        var config = new GenerationConfiguration
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today,
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            Deterministic = true,
            Seed = 42
        };

        var generator = RandomGeneratorFactory.Create(config);

        generator.Should().BeOfType<DeterministicRandomGenerator>();
        generator.IsDeterministic.Should().BeTrue();
        generator.Seed.Should().Be(42);
    }

    [Fact]
    public void Create_DeterministicConfigWithoutSeed_ComputesSeedFromConfig()
    {
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            Period = IntervalPeriod.FifteenMinute,
            BusinessType = "Manufacturing",
            Deterministic = true,
            Seed = null
        };

        var generator = RandomGeneratorFactory.Create(config);

        generator.Should().BeOfType<DeterministicRandomGenerator>();
        generator.IsDeterministic.Should().BeTrue();
        generator.Seed.Should().NotBeNull();
    }

    [Fact]
    public void Create_SameConfigWithoutSeed_ProducesSameSeed()
    {
        var config1 = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            Period = IntervalPeriod.FifteenMinute,
            BusinessType = "Office",
            Deterministic = true
        };

        var config2 = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            Period = IntervalPeriod.FifteenMinute,
            BusinessType = "Office",
            Deterministic = true
        };

        var generator1 = RandomGeneratorFactory.Create(config1);
        var generator2 = RandomGeneratorFactory.Create(config2);

        generator1.Seed.Should().Be(generator2.Seed);
    }

    [Fact]
    public void Create_DifferentConfigs_ProduceDifferentSeeds()
    {
        var config1 = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            Period = IntervalPeriod.FifteenMinute,
            BusinessType = "Office",
            Deterministic = true
        };

        var config2 = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            Period = IntervalPeriod.FifteenMinute,
            BusinessType = "Manufacturing", // Different business type
            Deterministic = true
        };

        var generator1 = RandomGeneratorFactory.Create(config1);
        var generator2 = RandomGeneratorFactory.Create(config2);

        generator1.Seed.Should().NotBe(generator2.Seed);
    }

    [Fact]
    public void Create_NullConfiguration_ThrowsArgumentNullException()
    {
        var act = () => RandomGeneratorFactory.Create(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region CreateDeterministic Tests

    [Fact]
    public void CreateDeterministic_ReturnsGeneratorWithSpecifiedSeed()
    {
        const int seed = 12345;

        var generator = RandomGeneratorFactory.CreateDeterministic(seed);

        generator.Should().BeOfType<DeterministicRandomGenerator>();
        generator.Seed.Should().Be(seed);
    }

    [Fact]
    public void CreateDeterministic_SameSeed_ProducesSameSequence()
    {
        var generator1 = RandomGeneratorFactory.CreateDeterministic(42);
        var generator2 = RandomGeneratorFactory.CreateDeterministic(42);

        var sequence1 = Enumerable.Range(0, 20).Select(_ => generator1.NextDouble()).ToList();
        var sequence2 = Enumerable.Range(0, 20).Select(_ => generator2.NextDouble()).ToList();

        sequence1.Should().BeEquivalentTo(sequence2, options => options.WithStrictOrdering());
    }

    #endregion

    #region CreateNonDeterministic Tests

    [Fact]
    public void CreateNonDeterministic_ReturnsNonDeterministicGenerator()
    {
        var generator = RandomGeneratorFactory.CreateNonDeterministic();

        generator.Should().BeOfType<NonDeterministicRandomGenerator>();
        generator.IsDeterministic.Should().BeFalse();
        generator.Seed.Should().BeNull();
    }

    #endregion

    #region Reproducibility Tests

    [Fact]
    public void DeterministicMode_SameConfig_ProducesIdenticalSequences()
    {
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 1),
            EndDate = new DateTime(2024, 6, 30),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Retail",
            MeterCount = 5,
            Deterministic = true,
            Seed = 999
        };

        var generator1 = RandomGeneratorFactory.Create(config);
        var generator2 = RandomGeneratorFactory.Create(config);

        var sequence1 = Enumerable.Range(0, 1000).Select(_ => generator1.NextDouble()).ToList();
        var sequence2 = Enumerable.Range(0, 1000).Select(_ => generator2.NextDouble()).ToList();

        sequence1.Should().BeEquivalentTo(sequence2, options => options.WithStrictOrdering());
    }

    #endregion
}
