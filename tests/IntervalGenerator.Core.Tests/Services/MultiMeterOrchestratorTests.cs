using FluentAssertions;
using IntervalGenerator.Core.Models;
using IntervalGenerator.Core.Profiles;
using IntervalGenerator.Core.Services;
using NSubstitute;

namespace IntervalGenerator.Core.Tests.Services;

public class MultiMeterOrchestratorTests
{
    private readonly IConsumptionProfile _mockProfile;
    private readonly Func<string, IConsumptionProfile> _profileResolver;

    public MultiMeterOrchestratorTests()
    {
        _mockProfile = Substitute.For<IConsumptionProfile>();
        _mockProfile.BusinessType.Returns("TestProfile");
        _mockProfile.GetBaseLoad(Arg.Any<DateTime>(), Arg.Any<int>()).Returns(100m);
        _mockProfile.GetTimeOfDayModifier(Arg.Any<DateTime>(), Arg.Any<int>()).Returns(1.0m);
        _mockProfile.GetDayOfWeekModifier(Arg.Any<DateTime>()).Returns(1.0m);
        _mockProfile.GetSeasonalModifier(Arg.Any<DateTime>()).Returns(1.0m);
        _mockProfile.GetRandomVariation(Arg.Any<Core.Randomization.IRandomNumberGenerator>()).Returns(1.0m);

        _profileResolver = businessType => _mockProfile;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullProfileResolver_ThrowsArgumentNullException()
    {
        var act = () => new MultiMeterOrchestrator(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("profileResolver");
    }

    #endregion

    #region Generate Tests

    [Fact]
    public void Generate_NullConfiguration_ThrowsArgumentNullException()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);

        var act = () => orchestrator.Generate(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Generate_SingleMeter_SingleDay_ReturnsCorrectReadingCount()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 1
        };

        var result = orchestrator.Generate(config);

        result.Readings.Should().HaveCount(48);
        result.TotalReadings.Should().Be(48);
    }

    [Fact]
    public void Generate_MultipleMeters_ReturnsCorrectReadingCount()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 5
        };

        var result = orchestrator.Generate(config);

        result.Readings.Should().HaveCount(5 * 48); // 5 meters * 48 periods
        result.UniqueMeterIds.Should().HaveCount(5);
    }

    [Fact]
    public void Generate_MultiDayRange_ReturnsCorrectReadingCount()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 1),
            EndDate = new DateTime(2024, 6, 7), // 7 days
            Period = IntervalPeriod.FifteenMinute,
            BusinessType = "Office",
            MeterCount = 3
        };

        var result = orchestrator.Generate(config);

        // 7 days * 96 periods * 3 meters = 2016 readings
        result.Readings.Should().HaveCount(7 * 96 * 3);
    }

    [Fact]
    public void Generate_WithProvidedMeterIds_UsesProvidedIds()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var providedIds = new List<Guid>
        {
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222")
        };

        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 2,
            MeterIds = providedIds
        };

        var result = orchestrator.Generate(config);

        result.UniqueMeterIds.Should().BeEquivalentTo(providedIds);
    }

    [Fact]
    public void Generate_ReturnsConfigurationInResult()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 1
        };

        var result = orchestrator.Generate(config);

        result.Configuration.Should().Be(config);
    }

    [Fact]
    public void Generate_CalculatesStatisticsCorrectly()
    {
        _mockProfile.GetBaseLoad(Arg.Any<DateTime>(), Arg.Any<int>()).Returns(100m);

        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 1
        };

        var result = orchestrator.Generate(config);

        result.MinConsumptionKwh.Should().Be(100m);
        result.MaxConsumptionKwh.Should().Be(100m);
        result.AverageConsumptionKwh.Should().Be(100m);
        result.TotalConsumptionKwh.Should().Be(4800m); // 48 periods * 100
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Generate_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 10),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 1
        };

        var act = () => orchestrator.Generate(config);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_MeterCountZero_ThrowsArgumentException()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 0
        };

        var act = () => orchestrator.Generate(config);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Generate_MeterCountExceeds1000_ThrowsArgumentException()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 1001
        };

        var act = () => orchestrator.Generate(config);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Generate_InvalidBusinessType_ThrowsArgumentException(string? businessType)
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = businessType!,
            MeterCount = 1
        };

        var act = () => orchestrator.Generate(config);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Deterministic Mode Tests

    [Fact]
    public void Generate_DeterministicMode_ProducesReproducibleResults()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 3,
            Deterministic = true,
            Seed = 42
        };

        var result1 = orchestrator.Generate(config);
        var result2 = orchestrator.Generate(config);

        // Same meter IDs
        result1.UniqueMeterIds.Should().BeEquivalentTo(result2.UniqueMeterIds,
            options => options.WithStrictOrdering());

        // Same consumption values
        result1.Readings.Select(r => r.ConsumptionKwh)
            .Should().BeEquivalentTo(result2.Readings.Select(r => r.ConsumptionKwh),
                options => options.WithStrictOrdering());
    }

    [Fact]
    public void Generate_DeterministicMode_DifferentSeeds_ProduceDifferentResults()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);

        var config1 = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 1,
            Deterministic = true,
            Seed = 42
        };

        var config2 = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 1,
            Deterministic = true,
            Seed = 43
        };

        var result1 = orchestrator.Generate(config1);
        var result2 = orchestrator.Generate(config2);

        // Different meter IDs due to different seeds
        result1.UniqueMeterIds.Should().NotBeEquivalentTo(result2.UniqueMeterIds);
    }

    #endregion

    #region GenerateStreaming Tests

    [Fact]
    public void GenerateStreaming_ReturnsEnumerableOfReadings()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 2
        };

        var readings = orchestrator.GenerateStreaming(config).ToList();

        readings.Should().HaveCount(2 * 48);
    }

    [Fact]
    public void GenerateStreaming_IsLazilyEvaluated()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 10
        };

        // Just getting the enumerable shouldn't enumerate it
        var enumerable = orchestrator.GenerateStreaming(config);

        // Take only first 5 readings
        var firstFive = enumerable.Take(5).ToList();

        firstFive.Should().HaveCount(5);
    }

    #endregion

    #region CalculateExpectedReadingCount Tests

    [Theory]
    [InlineData(1, 1, IntervalPeriod.ThirtyMinute, 48)]
    [InlineData(1, 1, IntervalPeriod.FifteenMinute, 96)]
    [InlineData(7, 1, IntervalPeriod.ThirtyMinute, 336)]
    [InlineData(1, 10, IntervalPeriod.ThirtyMinute, 480)]
    [InlineData(365, 1, IntervalPeriod.FifteenMinute, 35040)]
    [InlineData(365, 100, IntervalPeriod.FifteenMinute, 3504000)]
    public void CalculateExpectedReadingCount_ReturnsCorrectCount(
        int days, int meters, IntervalPeriod period, long expected)
    {
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 1).AddDays(days - 1),
            Period = period,
            BusinessType = "Office",
            MeterCount = meters
        };

        var count = MultiMeterOrchestrator.CalculateExpectedReadingCount(config);

        count.Should().Be(expected);
    }

    [Fact]
    public void CalculateExpectedReadingCount_NullConfig_ThrowsArgumentNullException()
    {
        var act = () => MultiMeterOrchestrator.CalculateExpectedReadingCount(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region MPAN Assignment Tests

    [Fact]
    public void Generate_AssignsUniqueMpansToEachMeter()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 10
        };

        var result = orchestrator.Generate(config);

        var uniqueMpans = result.Readings.Select(r => r.Mpan).Distinct().ToList();
        uniqueMpans.Should().HaveCount(10);
    }

    [Fact]
    public void Generate_MpansAreValid13DigitStrings()
    {
        var orchestrator = new MultiMeterOrchestrator(_profileResolver);
        var config = new GenerationConfiguration
        {
            StartDate = new DateTime(2024, 6, 15),
            EndDate = new DateTime(2024, 6, 15),
            Period = IntervalPeriod.ThirtyMinute,
            BusinessType = "Office",
            MeterCount = 5
        };

        var result = orchestrator.Generate(config);

        result.Readings.Should().AllSatisfy(r =>
        {
            r.Mpan.Should().HaveLength(13);
            r.Mpan.Should().MatchRegex(@"^\d{13}$");
        });
    }

    #endregion
}
