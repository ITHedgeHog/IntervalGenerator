using IntervalGenerator.Core.Models;
using IntervalGenerator.Core.Profiles;
using IntervalGenerator.Core.Randomization;
using IntervalGenerator.Core.Utilities;

namespace IntervalGenerator.Core.Services;

/// <summary>
/// Core engine for generating interval consumption readings.
/// </summary>
public class IntervalGeneratorEngine
{
    private readonly IConsumptionProfile _profile;
    private readonly IRandomNumberGenerator _randomGenerator;

    /// <summary>
    /// Initializes a new instance of the IntervalGeneratorEngine.
    /// </summary>
    /// <param name="profile">The consumption profile to use for generation.</param>
    /// <param name="randomGenerator">The random number generator for variation.</param>
    public IntervalGeneratorEngine(IConsumptionProfile profile, IRandomNumberGenerator randomGenerator)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _randomGenerator = randomGenerator ?? throw new ArgumentNullException(nameof(randomGenerator));
    }

    /// <summary>
    /// Generates interval readings for a single meter.
    /// </summary>
    /// <param name="meterId">The unique meter identifier.</param>
    /// <param name="mpan">The MPAN for the meter.</param>
    /// <param name="startDate">Start date (inclusive).</param>
    /// <param name="endDate">End date (inclusive).</param>
    /// <param name="period">The interval period.</param>
    /// <param name="measurementClass">The measurement class.</param>
    /// <returns>An enumerable of interval readings.</returns>
    public IEnumerable<IntervalReading> GenerateReadings(
        Guid meterId,
        string mpan,
        DateTime startDate,
        DateTime endDate,
        IntervalPeriod period,
        MeasurementClass measurementClass)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mpan);

        if (endDate < startDate)
        {
            throw new ArgumentException("End date must be greater than or equal to start date.", nameof(endDate));
        }

        int periodsPerDay = IntervalCalculator.GetPeriodsPerDay(period);

        // Iterate over each day in the range
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            // Generate readings for each period in the day
            for (int periodNumber = 1; periodNumber <= periodsPerDay; periodNumber++)
            {
                var timestamp = IntervalCalculator.GetPeriodStartTime(date, periodNumber, period);
                var consumption = CalculateConsumption(timestamp);

                yield return new IntervalReading
                {
                    MeterId = meterId,
                    Mpan = mpan,
                    Timestamp = timestamp,
                    Period = periodNumber,
                    ConsumptionKwh = consumption,
                    MeasurementClass = measurementClass,
                    QualityFlag = DataQualityFlag.Actual,
                    BusinessType = _profile.BusinessType
                };
            }
        }
    }

    /// <summary>
    /// Calculates the consumption for a specific timestamp using the profile modifiers.
    /// </summary>
    /// <param name="timestamp">The timestamp to calculate consumption for.</param>
    /// <returns>The calculated consumption in kWh.</returns>
    private decimal CalculateConsumption(DateTime timestamp)
    {
        int hour = timestamp.Hour;

        // Get all modifiers from the profile
        decimal baseLoad = _profile.GetBaseLoad(timestamp, hour);
        decimal timeOfDayModifier = _profile.GetTimeOfDayModifier(timestamp, hour);
        decimal dayOfWeekModifier = _profile.GetDayOfWeekModifier(timestamp);
        decimal seasonalModifier = _profile.GetSeasonalModifier(timestamp);
        decimal randomVariation = _profile.GetRandomVariation(_randomGenerator);

        // Calculate final consumption
        // Formula: BaseLoad × TimeOfDay × DayOfWeek × Seasonal × RandomVariation
        decimal consumption = baseLoad
            * timeOfDayModifier
            * dayOfWeekModifier
            * seasonalModifier
            * randomVariation;

        // Ensure non-negative and round to 2 decimal places
        return Math.Max(0, Math.Round(consumption, 2));
    }
}
