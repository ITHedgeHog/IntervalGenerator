namespace IntervalGenerator.Profiles;

/// <summary>
/// Consumption profile for office buildings.
/// Characteristics: 8am-6pm weekday operation, reduced weekends, seasonal HVAC variations.
/// </summary>
public sealed class OfficeProfile : BaseConsumptionProfile
{
    private const decimal BaseLoadKwh = 80m; // Base consumption per interval
    private const int OpeningHour = 8;
    private const int ClosingHour = 18; // 6 PM

    /// <inheritdoc />
    public override string BusinessType => "Office";

    /// <inheritdoc />
    public override decimal GetBaseLoad(DateTime dateTime, int hour)
    {
        // During business hours on weekdays
        if (IsWeekday(dateTime) && IsWithinBusinessHours(hour, OpeningHour, ClosingHour))
        {
            return BaseLoadKwh;
        }

        // Reduced load outside business hours and weekends
        return BaseLoadKwh * 0.2m; // 20% of base load
    }

    /// <inheritdoc />
    public override decimal GetTimeOfDayModifier(DateTime dateTime, int hour)
    {
        // Outside business hours - minimal consumption
        if (!IsWithinBusinessHours(hour, OpeningHour, ClosingHour))
            return 0.3m; // Minimal night/weekend consumption

        // Ramp up from 8am
        if (hour == OpeningHour)
            return GetRampUpFactor(hour, OpeningHour, rampDurationHours: 2);

        // Peak consumption 10am-4pm
        if (hour >= 10 && hour < 16)
            return 1.2m; // 20% above base

        // Ramp down from 4pm
        if (hour >= 16)
            return GetRampDownFactor(hour, ClosingHour, rampDurationHours: 2);

        // 8-10am steady increase
        return 0.8m;
    }

    /// <inheritdoc />
    public override decimal GetDayOfWeekModifier(DateTime dateTime)
    {
        return dateTime.DayOfWeek switch
        {
            DayOfWeek.Monday => 1.0m,
            DayOfWeek.Tuesday => 1.0m,
            DayOfWeek.Wednesday => 1.0m,
            DayOfWeek.Thursday => 1.0m,
            DayOfWeek.Friday => 0.95m, // Slightly lower on Friday
            DayOfWeek.Saturday => 0.2m, // 20% of weekday
            DayOfWeek.Sunday => 0.15m, // 15% of weekday
            _ => 1.0m
        };
    }

    /// <inheritdoc />
    public override decimal GetSeasonalModifier(DateTime dateTime)
    {
        int month = dateTime.Month;

        // Summer (Jun-Aug): Peak AC usage
        if (month >= 6 && month <= 8)
            return 1.25m;

        // Winter (Dec-Feb): Heating load
        if (month == 12 || month <= 2)
            return 1.15m;

        // Spring/Fall: Moderate
        if ((month >= 3 && month <= 5) || (month >= 9 && month <= 11))
            return 1.0m;

        return 1.0m;
    }

    /// <inheritdoc />
    public override decimal GetRandomVariation(IntervalGenerator.Core.Randomization.IRandomNumberGenerator randomGenerator)
    {
        // ±8% variation for offices
        double variation = randomGenerator.NextDouble() * 0.16 - 0.08;
        return (decimal)(1.0 + variation);
    }
}
