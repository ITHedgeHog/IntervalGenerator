using FluentAssertions;
using IntervalGenerator.Core.Models;
using IntervalGenerator.Core.Utilities;

namespace IntervalGenerator.Core.Tests.Utilities;

public class IntervalCalculatorTests
{
    #region CalculatePeriodNumber Tests

    [Theory]
    [InlineData(0, 0, IntervalPeriod.FifteenMinute, 1)]   // Midnight = period 1
    [InlineData(0, 15, IntervalPeriod.FifteenMinute, 2)]  // 00:15 = period 2
    [InlineData(0, 30, IntervalPeriod.FifteenMinute, 3)]  // 00:30 = period 3
    [InlineData(12, 0, IntervalPeriod.FifteenMinute, 49)] // Noon = period 49
    [InlineData(23, 45, IntervalPeriod.FifteenMinute, 96)] // 23:45 = period 96
    public void CalculatePeriodNumber_FifteenMinute_ReturnsCorrectPeriod(
        int hour, int minute, IntervalPeriod period, int expectedPeriod)
    {
        var dateTime = new DateTime(2024, 1, 15, hour, minute, 0);

        var result = IntervalCalculator.CalculatePeriodNumber(dateTime, period);

        result.Should().Be(expectedPeriod);
    }

    [Theory]
    [InlineData(0, 0, IntervalPeriod.ThirtyMinute, 1)]    // Midnight = period 1
    [InlineData(0, 30, IntervalPeriod.ThirtyMinute, 2)]   // 00:30 = period 2
    [InlineData(12, 0, IntervalPeriod.ThirtyMinute, 25)]  // Noon = period 25
    [InlineData(23, 30, IntervalPeriod.ThirtyMinute, 48)] // 23:30 = period 48
    public void CalculatePeriodNumber_ThirtyMinute_ReturnsCorrectPeriod(
        int hour, int minute, IntervalPeriod period, int expectedPeriod)
    {
        var dateTime = new DateTime(2024, 1, 15, hour, minute, 0);

        var result = IntervalCalculator.CalculatePeriodNumber(dateTime, period);

        result.Should().Be(expectedPeriod);
    }

    [Fact]
    public void CalculatePeriodNumber_InvalidPeriod_ThrowsArgumentException()
    {
        var dateTime = new DateTime(2024, 1, 15, 12, 0, 0);
        var invalidPeriod = (IntervalPeriod)99;

        var act = () => IntervalCalculator.CalculatePeriodNumber(dateTime, invalidPeriod);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("intervalPeriod");
    }

    #endregion

    #region GetPeriodStartTime Tests

    [Theory]
    [InlineData(1, IntervalPeriod.FifteenMinute, 0, 0)]   // Period 1 starts at 00:00
    [InlineData(2, IntervalPeriod.FifteenMinute, 0, 15)]  // Period 2 starts at 00:15
    [InlineData(49, IntervalPeriod.FifteenMinute, 12, 0)] // Period 49 starts at 12:00
    [InlineData(96, IntervalPeriod.FifteenMinute, 23, 45)] // Period 96 starts at 23:45
    public void GetPeriodStartTime_FifteenMinute_ReturnsCorrectTime(
        int periodNumber, IntervalPeriod period, int expectedHour, int expectedMinute)
    {
        var date = new DateTime(2024, 1, 15);

        var result = IntervalCalculator.GetPeriodStartTime(date, periodNumber, period);

        result.Hour.Should().Be(expectedHour);
        result.Minute.Should().Be(expectedMinute);
        result.Second.Should().Be(0);
    }

    [Theory]
    [InlineData(1, IntervalPeriod.ThirtyMinute, 0, 0)]    // Period 1 starts at 00:00
    [InlineData(2, IntervalPeriod.ThirtyMinute, 0, 30)]   // Period 2 starts at 00:30
    [InlineData(25, IntervalPeriod.ThirtyMinute, 12, 0)]  // Period 25 starts at 12:00
    [InlineData(48, IntervalPeriod.ThirtyMinute, 23, 30)] // Period 48 starts at 23:30
    public void GetPeriodStartTime_ThirtyMinute_ReturnsCorrectTime(
        int periodNumber, IntervalPeriod period, int expectedHour, int expectedMinute)
    {
        var date = new DateTime(2024, 1, 15);

        var result = IntervalCalculator.GetPeriodStartTime(date, periodNumber, period);

        result.Hour.Should().Be(expectedHour);
        result.Minute.Should().Be(expectedMinute);
    }

    #endregion

    #region GetPeriodEndTime Tests

    [Theory]
    [InlineData(1, IntervalPeriod.FifteenMinute, 0, 15)]  // Period 1 ends at 00:15
    [InlineData(95, IntervalPeriod.FifteenMinute, 23, 45)] // Period 95 ends at 23:45
    [InlineData(96, IntervalPeriod.FifteenMinute, 0, 0)]  // Period 96 ends at 00:00 (next day)
    public void GetPeriodEndTime_FifteenMinute_ReturnsCorrectTime(
        int periodNumber, IntervalPeriod period, int expectedHour, int expectedMinute)
    {
        var date = new DateTime(2024, 1, 15);

        var result = IntervalCalculator.GetPeriodEndTime(date, periodNumber, period);

        result.Hour.Should().Be(expectedHour);
        result.Minute.Should().Be(expectedMinute);
    }

    #endregion

    #region GetPeriodsPerDay Tests

    [Fact]
    public void GetPeriodsPerDay_FifteenMinute_Returns96()
    {
        var result = IntervalCalculator.GetPeriodsPerDay(IntervalPeriod.FifteenMinute);

        result.Should().Be(96);
    }

    [Fact]
    public void GetPeriodsPerDay_ThirtyMinute_Returns48()
    {
        var result = IntervalCalculator.GetPeriodsPerDay(IntervalPeriod.ThirtyMinute);

        result.Should().Be(48);
    }

    [Fact]
    public void GetPeriodsPerDay_InvalidPeriod_ThrowsArgumentException()
    {
        var invalidPeriod = (IntervalPeriod)99;

        var act = () => IntervalCalculator.GetPeriodsPerDay(invalidPeriod);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region IsValidPeriod Tests

    [Theory]
    [InlineData(1, IntervalPeriod.FifteenMinute, true)]
    [InlineData(96, IntervalPeriod.FifteenMinute, true)]
    [InlineData(0, IntervalPeriod.FifteenMinute, false)]
    [InlineData(97, IntervalPeriod.FifteenMinute, false)]
    [InlineData(1, IntervalPeriod.ThirtyMinute, true)]
    [InlineData(48, IntervalPeriod.ThirtyMinute, true)]
    [InlineData(49, IntervalPeriod.ThirtyMinute, false)]
    public void IsValidPeriod_ReturnsExpectedResult(
        int periodNumber, IntervalPeriod period, bool expected)
    {
        var result = IntervalCalculator.IsValidPeriod(periodNumber, period);

        result.Should().Be(expected);
    }

    #endregion

    #region Round-trip Tests

    [Theory]
    [InlineData(IntervalPeriod.FifteenMinute)]
    [InlineData(IntervalPeriod.ThirtyMinute)]
    public void PeriodStartTime_RoundTrip_CalculatesPeriodNumberCorrectly(IntervalPeriod period)
    {
        var date = new DateTime(2024, 6, 15);
        int periodsPerDay = IntervalCalculator.GetPeriodsPerDay(period);

        for (int periodNumber = 1; periodNumber <= periodsPerDay; periodNumber++)
        {
            var startTime = IntervalCalculator.GetPeriodStartTime(date, periodNumber, period);
            var calculatedPeriod = IntervalCalculator.CalculatePeriodNumber(startTime, period);

            calculatedPeriod.Should().Be(periodNumber,
                $"period {periodNumber} should round-trip correctly");
        }
    }

    #endregion
}
