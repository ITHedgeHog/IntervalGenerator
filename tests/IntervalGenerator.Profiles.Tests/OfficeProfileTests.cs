using FluentAssertions;
using IntervalGenerator.Core.Randomization;
using NSubstitute;

namespace IntervalGenerator.Profiles.Tests;

public class OfficeProfileTests
{
    private readonly OfficeProfile _profile;
    private readonly IRandomNumberGenerator _mockRng;

    public OfficeProfileTests()
    {
        _profile = new OfficeProfile();
        _mockRng = Substitute.For<IRandomNumberGenerator>();
        _mockRng.NextDouble().Returns(0.5); // Middle of range
    }

    #region BusinessType Tests

    [Fact]
    public void BusinessType_ReturnsOffice()
    {
        _profile.BusinessType.Should().Be("Office");
    }

    #endregion

    #region GetBaseLoad Tests

    [Theory]
    [InlineData(8)]  // Opening hour
    [InlineData(12)] // Midday
    [InlineData(17)] // Before closing
    public void GetBaseLoad_DuringBusinessHoursOnWeekday_ReturnsFullBaseLoad(int hour)
    {
        var weekday = new DateTime(2024, 6, 12); // Wednesday

        var baseLoad = _profile.GetBaseLoad(weekday, hour);

        baseLoad.Should().Be(80m);
    }

    [Theory]
    [InlineData(0)]  // Midnight
    [InlineData(7)]  // Before opening
    [InlineData(18)] // After closing
    [InlineData(23)] // Late night
    public void GetBaseLoad_OutsideBusinessHoursOnWeekday_ReturnsReducedLoad(int hour)
    {
        var weekday = new DateTime(2024, 6, 12); // Wednesday

        var baseLoad = _profile.GetBaseLoad(weekday, hour);

        baseLoad.Should().Be(16m); // 20% of 80
    }

    [Theory]
    [InlineData(DayOfWeek.Saturday)]
    [InlineData(DayOfWeek.Sunday)]
    public void GetBaseLoad_OnWeekend_ReturnsReducedLoad(DayOfWeek day)
    {
        var weekend = GetDateForDayOfWeek(day);

        var baseLoad = _profile.GetBaseLoad(weekend, 12); // Even during "business hours"

        baseLoad.Should().Be(16m); // 20% of 80
    }

    #endregion

    #region GetTimeOfDayModifier Tests

    [Theory]
    [InlineData(0, 0.3)]
    [InlineData(6, 0.3)]
    [InlineData(7, 0.3)]
    public void GetTimeOfDayModifier_BeforeBusinessHours_ReturnsMinimalModifier(int hour, decimal expected)
    {
        var date = new DateTime(2024, 6, 12); // Wednesday

        var modifier = _profile.GetTimeOfDayModifier(date, hour);

        modifier.Should().Be(expected);
    }

    [Theory]
    [InlineData(10, 1.2)]
    [InlineData(12, 1.2)]
    [InlineData(15, 1.2)]
    public void GetTimeOfDayModifier_DuringPeakHours_ReturnsPeakModifier(int hour, decimal expected)
    {
        var date = new DateTime(2024, 6, 12); // Wednesday

        var modifier = _profile.GetTimeOfDayModifier(date, hour);

        modifier.Should().Be(expected);
    }

    [Theory]
    [InlineData(18, 0.3)]
    [InlineData(20, 0.3)]
    [InlineData(23, 0.3)]
    public void GetTimeOfDayModifier_AfterBusinessHours_ReturnsMinimalModifier(int hour, decimal expected)
    {
        var date = new DateTime(2024, 6, 12); // Wednesday

        var modifier = _profile.GetTimeOfDayModifier(date, hour);

        modifier.Should().Be(expected);
    }

    #endregion

    #region GetDayOfWeekModifier Tests

    [Theory]
    [InlineData(DayOfWeek.Monday, 1.0)]
    [InlineData(DayOfWeek.Tuesday, 1.0)]
    [InlineData(DayOfWeek.Wednesday, 1.0)]
    [InlineData(DayOfWeek.Thursday, 1.0)]
    public void GetDayOfWeekModifier_MondayToThursday_ReturnsFullModifier(DayOfWeek day, decimal expected)
    {
        var date = GetDateForDayOfWeek(day);

        var modifier = _profile.GetDayOfWeekModifier(date);

        modifier.Should().Be(expected);
    }

    [Fact]
    public void GetDayOfWeekModifier_Friday_ReturnsSlightlyReduced()
    {
        var friday = GetDateForDayOfWeek(DayOfWeek.Friday);

        var modifier = _profile.GetDayOfWeekModifier(friday);

        modifier.Should().Be(0.95m);
    }

    [Fact]
    public void GetDayOfWeekModifier_Saturday_ReturnsWeekendModifier()
    {
        var saturday = GetDateForDayOfWeek(DayOfWeek.Saturday);

        var modifier = _profile.GetDayOfWeekModifier(saturday);

        modifier.Should().Be(0.2m);
    }

    [Fact]
    public void GetDayOfWeekModifier_Sunday_ReturnsLowestModifier()
    {
        var sunday = GetDateForDayOfWeek(DayOfWeek.Sunday);

        var modifier = _profile.GetDayOfWeekModifier(sunday);

        modifier.Should().Be(0.15m);
    }

    #endregion

    #region GetSeasonalModifier Tests

    [Theory]
    [InlineData(6)]  // June
    [InlineData(7)]  // July
    [InlineData(8)]  // August
    public void GetSeasonalModifier_Summer_ReturnsPeakModifier(int month)
    {
        var date = new DateTime(2024, month, 15);

        var modifier = _profile.GetSeasonalModifier(date);

        modifier.Should().Be(1.25m);
    }

    [Theory]
    [InlineData(12)] // December
    [InlineData(1)]  // January
    [InlineData(2)]  // February
    public void GetSeasonalModifier_Winter_ReturnsHeatingModifier(int month)
    {
        var date = new DateTime(2024, month, 15);

        var modifier = _profile.GetSeasonalModifier(date);

        modifier.Should().Be(1.15m);
    }

    [Theory]
    [InlineData(3)]  // March
    [InlineData(4)]  // April
    [InlineData(5)]  // May
    [InlineData(9)]  // September
    [InlineData(10)] // October
    [InlineData(11)] // November
    public void GetSeasonalModifier_SpringAndFall_ReturnsNeutralModifier(int month)
    {
        var date = new DateTime(2024, month, 15);

        var modifier = _profile.GetSeasonalModifier(date);

        modifier.Should().Be(1.0m);
    }

    #endregion

    #region GetRandomVariation Tests

    [Fact]
    public void GetRandomVariation_ReturnsValueWithin8PercentOfOne()
    {
        // Test with various random values
        for (double randomValue = 0.0; randomValue <= 1.0; randomValue += 0.1)
        {
            _mockRng.NextDouble().Returns(randomValue);

            var variation = _profile.GetRandomVariation(_mockRng);

            variation.Should().BeInRange(0.92m, 1.08m);
        }
    }

    [Fact]
    public void GetRandomVariation_AtRandomZero_ReturnsLowerBound()
    {
        _mockRng.NextDouble().Returns(0.0);

        var variation = _profile.GetRandomVariation(_mockRng);

        variation.Should().Be(0.92m); // 1 - 0.08
    }

    [Fact]
    public void GetRandomVariation_AtRandomOne_ReturnsUpperBound()
    {
        _mockRng.NextDouble().Returns(1.0);

        var variation = _profile.GetRandomVariation(_mockRng);

        variation.Should().Be(1.08m); // 1 + 0.08
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullConsumptionCalculation_PeakWeekdaySummer_ReturnsHighConsumption()
    {
        // Wednesday in July at noon
        var date = new DateTime(2024, 7, 10, 12, 0, 0);
        _mockRng.NextDouble().Returns(0.5); // Neutral variation

        var baseLoad = _profile.GetBaseLoad(date, 12);
        var timeOfDay = _profile.GetTimeOfDayModifier(date, 12);
        var dayOfWeek = _profile.GetDayOfWeekModifier(date);
        var seasonal = _profile.GetSeasonalModifier(date);
        var variation = _profile.GetRandomVariation(_mockRng);

        var totalConsumption = baseLoad * timeOfDay * dayOfWeek * seasonal * variation;

        // 80 * 1.2 * 1.0 * 1.25 * 1.0 = 120
        totalConsumption.Should().Be(120m);
    }

    [Fact]
    public void FullConsumptionCalculation_WeekendNight_ReturnsLowConsumption()
    {
        // Sunday at 2am in April
        var date = new DateTime(2024, 4, 14, 2, 0, 0);
        _mockRng.NextDouble().Returns(0.5);

        var baseLoad = _profile.GetBaseLoad(date, 2);
        var timeOfDay = _profile.GetTimeOfDayModifier(date, 2);
        var dayOfWeek = _profile.GetDayOfWeekModifier(date);
        var seasonal = _profile.GetSeasonalModifier(date);
        var variation = _profile.GetRandomVariation(_mockRng);

        var totalConsumption = baseLoad * timeOfDay * dayOfWeek * seasonal * variation;

        // 16 * 0.3 * 0.15 * 1.0 * 1.0 = 0.72
        totalConsumption.Should().Be(0.72m);
    }

    #endregion

    #region Helper Methods

    private static DateTime GetDateForDayOfWeek(DayOfWeek targetDay)
    {
        // Start from a known Monday (June 10, 2024)
        var monday = new DateTime(2024, 6, 10);
        int daysToAdd = ((int)targetDay - (int)DayOfWeek.Monday + 7) % 7;
        return monday.AddDays(daysToAdd);
    }

    #endregion
}
