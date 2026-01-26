namespace IntervalGenerator.Core.Profiles;

/// <summary>
/// Defines the consumption characteristics for a specific business type.
/// </summary>
public interface IConsumptionProfile
{
    /// <summary>
    /// Gets the name of the business type this profile represents.
    /// </summary>
    string BusinessType { get; }

    /// <summary>
    /// Gets the base load consumption in kWh for the given date and hour.
    /// </summary>
    /// <param name="dateTime">The date to calculate base load for.</param>
    /// <param name="hour">The hour of day (0-23).</param>
    /// <returns>Base load in kWh (non-negative).</returns>
    decimal GetBaseLoad(DateTime dateTime, int hour);

    /// <summary>
    /// Gets the time-of-day modifier (multiplier) for consumption.
    /// Used to model peak and off-peak periods.
    /// </summary>
    /// <param name="dateTime">The date.</param>
    /// <param name="hour">The hour of day (0-23).</param>
    /// <returns>Multiplier (typically 0.0 to 2.0).</returns>
    decimal GetTimeOfDayModifier(DateTime dateTime, int hour);

    /// <summary>
    /// Gets the day-of-week modifier for consumption.
    /// Used to model weekday vs weekend variations.
    /// </summary>
    /// <param name="dateTime">The date.</param>
    /// <returns>Multiplier (typically 0.5 to 1.5).</returns>
    decimal GetDayOfWeekModifier(DateTime dateTime);

    /// <summary>
    /// Gets the seasonal modifier for consumption.
    /// Used to model summer/winter variations.
    /// </summary>
    /// <param name="dateTime">The date.</param>
    /// <returns>Multiplier (typically 0.8 to 1.2).</returns>
    decimal GetSeasonalModifier(DateTime dateTime);

    /// <summary>
    /// Gets the random variation/noise as a multiplier factor.
    /// Should return a value close to 1.0 with some variance.
    /// </summary>
    /// <param name="randomGenerator">The random number generator to use.</param>
    /// <returns>Multiplier (typically 0.85 to 1.15).</returns>
    decimal GetRandomVariation(Randomization.IRandomNumberGenerator randomGenerator);
}
