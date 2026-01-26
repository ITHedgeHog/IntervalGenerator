namespace IntervalGenerator.Profiles;

/// <summary>
/// Consumption profile for educational institutions (schools, universities).
/// Characteristics: Term-time operation, scheduled occupancy, academic calendars.
/// </summary>
public sealed class EducationalInstitutionProfile : BaseConsumptionProfile
{
    private const decimal BaseLoadKwh = 120m; // Moderate baseline
    private const int OpeningHour = 8;
    private const int ClosingHour = 18;

    /// <inheritdoc />
    public override string BusinessType => "Educational";

    /// <inheritdoc />
    public override decimal GetBaseLoad(DateTime dateTime, int hour)
    {
        // Academic year: Sep-Jun
        // Reduced load during summer break (Jul-Aug)
        bool isTermTime = IsTermTime(dateTime);

        if (!isTermTime)
            return BaseLoadKwh * 0.3m; // Minimal summer load

        // During term time, consumption during school hours
        if (IsWithinBusinessHours(hour, OpeningHour, ClosingHour))
            return BaseLoadKwh;

        // Outside school hours
        return BaseLoadKwh * 0.25m;
    }

    /// <inheritdoc />
    public override decimal GetTimeOfDayModifier(DateTime dateTime, int hour)
    {
        // During term time only
        if (!IsTermTime(dateTime))
            return 0.3m; // Minimal consumption during breaks

        // Outside school hours: minimal
        if (!IsWithinBusinessHours(hour, OpeningHour, ClosingHour))
            return 0.3m;

        // Opening ramp (8-9am)
        if (hour == OpeningHour)
            return GetRampUpFactor(hour, OpeningHour, rampDurationHours: 1);

        // Peak hours (10am-3pm): Classes in session
        if (hour >= 10 && hour < 15)
            return 1.3m; // 30% above baseline

        // Afternoon classes (3-5pm)
        if (hour >= 15 && hour < 17)
            return 1.15m;

        // Closing hours (5-6pm)
        if (hour >= 17)
            return GetRampDownFactor(hour, ClosingHour, rampDurationHours: 1);

        // Morning transition
        return 0.9m;
    }

    /// <inheritdoc />
    public override decimal GetDayOfWeekModifier(DateTime dateTime)
    {
        // During summer break: no variation
        if (!IsTermTime(dateTime))
            return 1.0m;

        return dateTime.DayOfWeek switch
        {
            DayOfWeek.Monday => 1.0m,
            DayOfWeek.Tuesday => 1.05m,
            DayOfWeek.Wednesday => 1.05m,
            DayOfWeek.Thursday => 1.0m,
            DayOfWeek.Friday => 0.95m,   // Lighter Friday (many leave early)
            DayOfWeek.Saturday => 0.4m,  // Minimal weekend operations
            DayOfWeek.Sunday => 0.3m,    // Very minimal Sunday
            _ => 1.0m
        };
    }

    /// <inheritdoc />
    public override decimal GetSeasonalModifier(DateTime dateTime)
    {
        int month = dateTime.Month;

        // Out of term time: Very low modifier
        if (!IsTermTime(dateTime))
            return 0.5m;

        // Summer term (May-Jun): Cooling load
        if (month >= 5 && month <= 6)
            return 1.15m;

        // Winter term (Jan-Feb): Heating load
        if (month <= 2)
            return 1.20m;

        // Fall term (Sep-Oct): Moderate
        if (month >= 9 && month <= 10)
            return 1.05m;

        // Spring term (Mar-Apr): Moderate
        if (month >= 3 && month <= 4)
            return 1.0m;

        return 1.0m;
    }

    /// <inheritdoc />
    public override decimal GetRandomVariation(IntervalGenerator.Core.Randomization.IRandomNumberGenerator randomGenerator)
    {
        // Educational institutions have moderate variation
        // ±10% variation
        double variation = randomGenerator.NextDouble() * 0.20 - 0.10;
        return (decimal)(1.0 + variation);
    }

    /// <summary>
    /// Determines if a date falls within academic term time.
    /// Assumes: September-June (Sept=1, June=6 of academic year)
    /// </summary>
    private static bool IsTermTime(DateTime dateTime)
    {
        int month = dateTime.Month;
        // Sept (9) through June (6) is term time
        // July (7) and August (8) are summer break
        return month != 7 && month != 8;
    }
}
