using FluentAssertions;
using IntervalGenerator.Core.Models;
using IntervalGenerator.Core.Profiles;
using IntervalGenerator.Core.Randomization;
using IntervalGenerator.Core.Services;
using NSubstitute;

namespace IntervalGenerator.Core.Tests.Services;

public class IntervalGeneratorEngineTests
{
    private readonly IConsumptionProfile _mockProfile;
    private readonly IRandomNumberGenerator _mockRandomGenerator;

    public IntervalGeneratorEngineTests()
    {
        _mockProfile = Substitute.For<IConsumptionProfile>();
        _mockRandomGenerator = Substitute.For<IRandomNumberGenerator>();

        // Default setup for mock profile
        _mockProfile.BusinessType.Returns("TestProfile");
        _mockProfile.GetBaseLoad(Arg.Any<DateTime>(), Arg.Any<int>()).Returns(100m);
        _mockProfile.GetTimeOfDayModifier(Arg.Any<DateTime>(), Arg.Any<int>()).Returns(1.0m);
        _mockProfile.GetDayOfWeekModifier(Arg.Any<DateTime>()).Returns(1.0m);
        _mockProfile.GetSeasonalModifier(Arg.Any<DateTime>()).Returns(1.0m);
        _mockProfile.GetRandomVariation(Arg.Any<IRandomNumberGenerator>()).Returns(1.0m);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullProfile_ThrowsArgumentNullException()
    {
        var act = () => new IntervalGeneratorEngine(null!, _mockRandomGenerator);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("profile");
    }

    [Fact]
    public void Constructor_NullRandomGenerator_ThrowsArgumentNullException()
    {
        var act = () => new IntervalGeneratorEngine(_mockProfile, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("randomGenerator");
    }

    #endregion

    #region GenerateReadings Tests

    [Fact]
    public void GenerateReadings_SingleDay_FifteenMinute_Returns96Readings()
    {
        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var date = new DateTime(2024, 6, 15);

        var readings = engine.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.FifteenMinute,
            MeasurementClass.AI).ToList();

        readings.Should().HaveCount(96);
    }

    [Fact]
    public void GenerateReadings_SingleDay_ThirtyMinute_Returns48Readings()
    {
        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var date = new DateTime(2024, 6, 15);

        var readings = engine.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        readings.Should().HaveCount(48);
    }

    [Fact]
    public void GenerateReadings_SevenDays_FifteenMinute_Returns672Readings()
    {
        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var startDate = new DateTime(2024, 6, 1);
        var endDate = new DateTime(2024, 6, 7); // 7 days

        var readings = engine.GenerateReadings(
            meterId, mpan, startDate, endDate,
            IntervalPeriod.FifteenMinute,
            MeasurementClass.AI).ToList();

        readings.Should().HaveCount(7 * 96); // 672 readings
    }

    [Fact]
    public void GenerateReadings_SetsCorrectMeterId()
    {
        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var date = new DateTime(2024, 6, 15);

        var readings = engine.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        readings.Should().AllSatisfy(r => r.MeterId.Should().Be(meterId));
    }

    [Fact]
    public void GenerateReadings_SetsCorrectMpan()
    {
        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "9876543210123";
        var date = new DateTime(2024, 6, 15);

        var readings = engine.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        readings.Should().AllSatisfy(r => r.Mpan.Should().Be(mpan));
    }

    [Fact]
    public void GenerateReadings_SetsCorrectMeasurementClass()
    {
        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var date = new DateTime(2024, 6, 15);

        var readings = engine.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AE).ToList();

        readings.Should().AllSatisfy(r => r.MeasurementClass.Should().Be(MeasurementClass.AE));
    }

    [Fact]
    public void GenerateReadings_SetsCorrectBusinessType()
    {
        _mockProfile.BusinessType.Returns("CustomBusiness");
        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var date = new DateTime(2024, 6, 15);

        var readings = engine.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        readings.Should().AllSatisfy(r => r.BusinessType.Should().Be("CustomBusiness"));
    }

    [Fact]
    public void GenerateReadings_PeriodsAreSequential()
    {
        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var date = new DateTime(2024, 6, 15);

        var readings = engine.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        var periods = readings.Select(r => r.Period).ToList();
        periods.Should().BeEquivalentTo(Enumerable.Range(1, 48));
    }

    [Fact]
    public void GenerateReadings_TimestampsAreCorrect()
    {
        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var date = new DateTime(2024, 6, 15);

        var readings = engine.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        // First reading should be at midnight
        readings[0].Timestamp.Should().Be(new DateTime(2024, 6, 15, 0, 0, 0));
        // Second reading should be at 00:30
        readings[1].Timestamp.Should().Be(new DateTime(2024, 6, 15, 0, 30, 0));
        // Last reading should be at 23:30
        readings[47].Timestamp.Should().Be(new DateTime(2024, 6, 15, 23, 30, 0));
    }

    [Fact]
    public void GenerateReadings_AppliesAllModifiers()
    {
        _mockProfile.GetBaseLoad(Arg.Any<DateTime>(), Arg.Any<int>()).Returns(100m);
        _mockProfile.GetTimeOfDayModifier(Arg.Any<DateTime>(), Arg.Any<int>()).Returns(1.5m);
        _mockProfile.GetDayOfWeekModifier(Arg.Any<DateTime>()).Returns(0.8m);
        _mockProfile.GetSeasonalModifier(Arg.Any<DateTime>()).Returns(1.2m);
        _mockProfile.GetRandomVariation(Arg.Any<IRandomNumberGenerator>()).Returns(1.1m);

        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var date = new DateTime(2024, 6, 15);

        var readings = engine.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        // Expected: 100 * 1.5 * 0.8 * 1.2 * 1.1 = 158.4
        readings.Should().AllSatisfy(r =>
            r.ConsumptionKwh.Should().Be(158.4m));
    }

    [Fact]
    public void GenerateReadings_ConsumptionIsNonNegative()
    {
        // Even with low modifiers, consumption should never be negative
        _mockProfile.GetBaseLoad(Arg.Any<DateTime>(), Arg.Any<int>()).Returns(10m);
        _mockProfile.GetTimeOfDayModifier(Arg.Any<DateTime>(), Arg.Any<int>()).Returns(0.1m);
        _mockProfile.GetDayOfWeekModifier(Arg.Any<DateTime>()).Returns(0.1m);
        _mockProfile.GetSeasonalModifier(Arg.Any<DateTime>()).Returns(0.1m);
        _mockProfile.GetRandomVariation(Arg.Any<IRandomNumberGenerator>()).Returns(0.5m);

        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var date = new DateTime(2024, 6, 15);

        var readings = engine.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        readings.Should().AllSatisfy(r =>
            r.ConsumptionKwh.Should().BeGreaterThanOrEqualTo(0));
    }

    [Fact]
    public void GenerateReadings_ConsumptionIsRoundedTo2DecimalPlaces()
    {
        _mockProfile.GetBaseLoad(Arg.Any<DateTime>(), Arg.Any<int>()).Returns(100.123456m);
        _mockProfile.GetTimeOfDayModifier(Arg.Any<DateTime>(), Arg.Any<int>()).Returns(1.111111m);

        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var date = new DateTime(2024, 6, 15);

        var readings = engine.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        readings.Should().AllSatisfy(r =>
        {
            var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(r.ConsumptionKwh)[3])[2];
            decimalPlaces.Should().BeLessThanOrEqualTo(2);
        });
    }

    [Fact]
    public void GenerateReadings_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var startDate = new DateTime(2024, 6, 15);
        var endDate = new DateTime(2024, 6, 10);

        var act = () => engine.GenerateReadings(
            meterId, mpan, startDate, endDate,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        act.Should().Throw<ArgumentException>()
            .WithParameterName("endDate");
    }

    [Fact]
    public void GenerateReadings_NullOrEmptyMpan_ThrowsArgumentException()
    {
        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var date = new DateTime(2024, 6, 15);

        var actNull = () => engine.GenerateReadings(
            meterId, null!, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        var actEmpty = () => engine.GenerateReadings(
            meterId, "", date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        actNull.Should().Throw<ArgumentException>();
        actEmpty.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateReadings_QualityFlagDefaultsToActual()
    {
        var engine = new IntervalGeneratorEngine(_mockProfile, _mockRandomGenerator);
        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var date = new DateTime(2024, 6, 15);

        var readings = engine.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        readings.Should().AllSatisfy(r =>
            r.QualityFlag.Should().Be(DataQualityFlag.Actual));
    }

    #endregion

    #region Deterministic Behavior Tests

    [Fact]
    public void GenerateReadings_WithDeterministicGenerator_ProducesReproducibleResults()
    {
        var deterministicGenerator = new DeterministicRandomGenerator(42);

        // Setup profile to use random variation
        _mockProfile.GetRandomVariation(Arg.Any<IRandomNumberGenerator>())
            .Returns(callInfo =>
            {
                var rng = callInfo.Arg<IRandomNumberGenerator>();
                return (decimal)(0.9 + rng.NextDouble() * 0.2);
            });

        var engine1 = new IntervalGeneratorEngine(_mockProfile, new DeterministicRandomGenerator(42));
        var engine2 = new IntervalGeneratorEngine(_mockProfile, new DeterministicRandomGenerator(42));

        var meterId = Guid.NewGuid();
        var mpan = "1234567890123";
        var date = new DateTime(2024, 6, 15);

        var readings1 = engine1.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        var readings2 = engine2.GenerateReadings(
            meterId, mpan, date, date,
            IntervalPeriod.ThirtyMinute,
            MeasurementClass.AI).ToList();

        readings1.Select(r => r.ConsumptionKwh)
            .Should().BeEquivalentTo(readings2.Select(r => r.ConsumptionKwh),
                options => options.WithStrictOrdering());
    }

    #endregion
}
