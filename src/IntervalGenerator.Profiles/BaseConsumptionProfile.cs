using IntervalGenerator.Core.Profiles;
using IntervalGenerator.Core.Randomization;

namespace IntervalGenerator.Profiles;

/// <summary>
/// Base class for consumption profiles providing common functionality.
/// </summary>
public abstract class BaseConsumptionProfile : IConsumptionProfile
{
    /// <inheritdoc />
    public abstract string BusinessType { get; }

    /// <inheritdoc />
    public abstract decimal GetBaseLoad(DateTime date, int hour);

    /// <inheritdoc />
    public abstract decimal GetTimeOfDayModifier(DateTime date, int hour);

    /// <inheritdoc />
    public abstract decimal GetDayOfWeekModifier(DateTime date);

    /// <inheritdoc />
    public virtual decimal GetSeasonalModifier(DateTime date)
    {
        // Default implementation: Northern hemisphere seasons
        int month = date.Month;

        // Summer (Jun-Aug): +20% for AC
        if (month >= 6 && month <= 8)
            return 1.20m;

        // Winter (Dec-Feb): +10% for heating
        if (month == 12 || month <= 2)
            return 1.10m;

        // Shoulder seasons: normal
        return 1.0m;
    }

    /// <inheritdoc />
    public virtual decimal GetRandomVariation(IRandomNumberGenerator randomGenerator)
    {
        // Default: ±10% variation
        double variation = randomGenerator.NextDouble() * 0.20 - 0.10; // -0.10 to +0.10
        return (decimal)(1.0 + variation);
    }

    /// <summary>
    /// Checks if a given hour is during business hours (inclusive).
    /// </summary>
    /// <param name="hour">The hour (0-23).</param>
    /// <param name="startHour">The start hour (inclusive).</param>
    /// <param name="endHour">The end hour (exclusive).</param>
    /// <returns>True if the hour is within business hours.</returns>
    protected bool IsWithinBusinessHours(int hour, int startHour, int endHour)
    {
        return hour >= startHour && hour < endHour;
    }

    /// <summary>
    /// Checks if the date is a weekday (Monday-Friday).
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if weekday; false if weekend.</returns>
    protected bool IsWeekday(DateTime date)
    {
        return date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday;
    }

    /// <summary>
    /// Gets a smooth ramp-up factor for gradual consumption increase.
    /// Used for ramping consumption up during opening hours.
    /// </summary>
    /// <param name="hour">The current hour.</param>
    /// <param name="startHour">The opening hour.</param>
    /// <param name="rampDurationHours">How many hours to ramp (default 1).</param>
    /// <returns>A multiplier between 0 and 1.</returns>
    protected decimal GetRampUpFactor(int hour, int startHour, int rampDurationHours = 1)
    {
        if (hour < startHour)
            return 0.0m;

        int hoursSinceOpen = hour - startHour;
        if (hoursSinceOpen >= rampDurationHours)
            return 1.0m;

        return (decimal)hoursSinceOpen / rampDurationHours;
    }

    /// <summary>
    /// Gets a smooth ramp-down factor for gradual consumption decrease.
    /// Used for ramping consumption down during closing hours.
    /// </summary>
    /// <param name="hour">The current hour.</param>
    /// <param name="endHour">The closing hour.</param>
    /// <param name="rampDurationHours">How many hours to ramp (default 1).</param>
    /// <returns>A multiplier between 0 and 1.</returns>
    protected decimal GetRampDownFactor(int hour, int endHour, int rampDurationHours = 1)
    {
        if (hour >= endHour)
            return 0.0m;

        int hoursUntilClose = endHour - hour;
        if (hoursUntilClose > rampDurationHours)
            return 1.0m;

        return (decimal)hoursUntilClose / rampDurationHours;
    }
}
