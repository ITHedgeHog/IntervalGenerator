namespace IntervalGenerator.Profiles;

/// <summary>
/// Consumption profile for retail facilities (stores, shopping centers).
/// Characteristics: Extended hours (9am-9pm), weekend peaks, high lighting/refrigeration loads.
/// </summary>
public class RetailProfile : BaseConsumptionProfile
{
    private const decimal BaseLoadKwh = 90m; // Moderate-high baseline
    private const int OpeningHour = 9;
    private const int ClosingHour = 21; // 9 PM

    /// <inheritdoc />
    public override string BusinessType => "Retail";

    /// <inheritdoc />
    public override decimal GetBaseLoad(DateTime date, int hour)
    {
        // During operating hours
        if (IsWithinBusinessHours(hour, OpeningHour, ClosingHour))
            return BaseLoadKwh;

        // Closed hours: minimal consumption (security, refrigeration)
        return BaseLoadKwh * 0.15m;
    }

    /// <inheritdoc />
    public override decimal GetTimeOfDayModifier(DateTime date, int hour)
    {
        // Before opening: minimal consumption
        if (hour < OpeningHour)
            return 0.2m;

        // Opening ramp (9-10am)
        if (hour == OpeningHour)
            return GetRampUpFactor(hour, OpeningHour, rampDurationHours: 1);

        // Mid-day (10am-5pm): Peak shopping hours
        if (hour >= 10 && hour < 17)
            return 1.3m; // 30% above baseline

        // Evening (5pm-8pm): Strong traffic, peak lighting
        if (hour >= 17 && hour < 20)
            return 1.25m;

        // Late evening ramp down (8-9pm)
        if (hour >= 20)
            return GetRampDownFactor(hour, ClosingHour, rampDurationHours: 1);

        // Standard hours
        return 1.1m;
    }

    /// <inheritdoc />
    public override decimal GetDayOfWeekModifier(DateTime date)
    {
        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => 0.9m,     // Slower start of week
            DayOfWeek.Tuesday => 0.92m,   // Building up
            DayOfWeek.Wednesday => 0.95m, // Mid-week
            DayOfWeek.Thursday => 1.0m,   // Strong
            DayOfWeek.Friday => 1.15m,    // Friday shopping surge
            DayOfWeek.Saturday => 1.25m,  // Peak weekend day
            DayOfWeek.Sunday => 1.10m,    // Good but slightly less than Saturday
            _ => 1.0m
        };
    }

    /// <inheritdoc />
    public override decimal GetSeasonalModifier(DateTime date)
    {
        int month = date.Month;

        // Holiday season (Nov-Dec): Peak shopping
        if (month == 11 || month == 12)
            return 1.35m;

        // Summer (Jun-Aug): Cooling load for facility + some shopping
        if (month >= 6 && month <= 8)
            return 1.15m;

        // Winter (Jan-Feb): Heating + after-holiday sales
        if (month <= 2)
            return 1.10m;

        // Spring/Fall: Moderate
        return 1.0m;
    }

    /// <inheritdoc />
    public override decimal GetRandomVariation(IntervalGenerator.Core.Randomization.IRandomNumberGenerator randomGenerator)
    {
        // Retail has higher variation due to customer traffic variability
        // ±12% variation
        double variation = randomGenerator.NextDouble() * 0.24 - 0.12;
        return (decimal)(1.0 + variation);
    }
}
