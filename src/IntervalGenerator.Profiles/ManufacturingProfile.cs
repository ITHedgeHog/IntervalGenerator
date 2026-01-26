namespace IntervalGenerator.Profiles;

/// <summary>
/// Consumption profile for manufacturing plants.
/// Characteristics: 24/7 operation, high baseline, minimal day-of-week variation, process-dependent loads.
/// </summary>
public sealed class ManufacturingProfile : BaseConsumptionProfile
{
    private const decimal BaseLoadKwh = 400m; // High baseline consumption

    /// <inheritdoc />
    public override string BusinessType => "Manufacturing";

    /// <inheritdoc />
    public override decimal GetBaseLoad(DateTime dateTime, int hour)
    {
        // Manufacturing typically runs 24/7 with consistent load
        return BaseLoadKwh;
    }

    /// <inheritdoc />
    public override decimal GetTimeOfDayModifier(DateTime dateTime, int hour)
    {
        // Manufacturing has relatively consistent consumption throughout the day
        // with slight variations for shift changes and maintenance windows

        // Maintenance window 2-3am: reduced to 70%
        if (hour == 2)
            return 0.7m;

        // Normal operation: slight variation by shift
        if (hour >= 6 && hour < 18)
            return 1.05m; // Day shift slightly higher

        if (hour >= 18 || hour < 2)
            return 1.0m; // Evening shift baseline

        return 0.85m; // Night shift reduced
    }

    /// <inheritdoc />
    public override decimal GetDayOfWeekModifier(DateTime dateTime)
    {
        // Manufacturing has minimal day-of-week variation (24/7 operation)
        return dateTime.DayOfWeek switch
        {
            DayOfWeek.Monday => 1.0m,
            DayOfWeek.Tuesday => 1.0m,
            DayOfWeek.Wednesday => 1.0m,
            DayOfWeek.Thursday => 1.0m,
            DayOfWeek.Friday => 1.0m,
            DayOfWeek.Saturday => 0.95m, // Slightly reduced weekends (minimal staffing)
            DayOfWeek.Sunday => 0.95m,   // Slightly reduced weekends
            _ => 1.0m
        };
    }

    /// <inheritdoc />
    public override decimal GetSeasonalModifier(DateTime dateTime)
    {
        int month = dateTime.Month;

        // Summer: Cooling loads for facility
        if (month >= 6 && month <= 8)
            return 1.12m;

        // Winter: Heating for facility
        if (month == 12 || month <= 2)
            return 1.08m;

        // Shoulder seasons
        return 1.0m;
    }

    /// <inheritdoc />
    public override decimal GetRandomVariation(IntervalGenerator.Core.Randomization.IRandomNumberGenerator randomGenerator)
    {
        // Manufacturing has lower variation due to continuous process operation
        // ±5% variation
        double variation = randomGenerator.NextDouble() * 0.10 - 0.05;
        return (decimal)(1.0 + variation);
    }
}
