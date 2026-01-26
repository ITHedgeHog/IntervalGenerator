namespace IntervalGenerator.Profiles;

/// <summary>
/// Consumption profile for data centers.
/// Characteristics: 24/7 operation, highly consistent load, significant cooling requirements.
/// </summary>
public sealed class DataCenterProfile : BaseConsumptionProfile
{
    private const decimal BaseLoadKwh = 500m; // High continuous load

    /// <inheritdoc />
    public override string BusinessType => "DataCenter";

    /// <inheritdoc />
    public override decimal GetBaseLoad(DateTime dateTime, int hour)
    {
        // Data centers run 24/7 with consistent load
        return BaseLoadKwh;
    }

    /// <inheritdoc />
    public override decimal GetTimeOfDayModifier(DateTime dateTime, int hour)
    {
        // Data centers have very consistent consumption
        // Slight variations for backup systems and scheduled maintenance

        // Maintenance window (3-4am UTC): Reduced load
        if (hour == 3)
            return 0.95m;

        // Standard operation: very consistent
        return 1.0m;
    }

    /// <inheritdoc />
    public override decimal GetDayOfWeekModifier(DateTime dateTime)
    {
        // Data centers operate identically 7 days a week
        return 1.0m;
    }

    /// <inheritdoc />
    public override decimal GetSeasonalModifier(DateTime dateTime)
    {
        int month = dateTime.Month;

        // Summer: Increased cooling load
        if (month >= 6 && month <= 8)
            return 1.20m; // 20% increase for AC

        // Winter: Reduced cooling, possible heating
        if (month == 12 || month <= 2)
            return 0.95m; // Less cooling needed

        // Spring/Fall: Optimal conditions
        if ((month >= 3 && month <= 5) || (month >= 9 && month <= 11))
            return 1.0m;

        return 1.0m;
    }

    /// <inheritdoc />
    public override decimal GetRandomVariation(IntervalGenerator.Core.Randomization.IRandomNumberGenerator randomGenerator)
    {
        // Data centers have very low variation due to controlled environment
        // ±2% variation only
        double variation = randomGenerator.NextDouble() * 0.04 - 0.02;
        return (decimal)(1.0 + variation);
    }
}
