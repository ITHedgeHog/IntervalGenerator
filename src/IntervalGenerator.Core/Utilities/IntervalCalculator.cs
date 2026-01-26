namespace IntervalGenerator.Core.Utilities;

/// <summary>
/// Utility class for interval calculations.
/// </summary>
public static class IntervalCalculator
{
    /// <summary>
    /// Calculates the period number (1-based) for a given time and interval period.
    /// </summary>
    /// <param name="dateTime">The date and time.</param>
    /// <param name="intervalPeriod">The interval period (15 or 30 minutes).</param>
    /// <returns>Period number (1-96 for 15-min, 1-48 for 30-min).</returns>
    /// <exception cref="ArgumentException">Thrown if intervalPeriod is invalid.</exception>
    public static int CalculatePeriodNumber(DateTime dateTime, Models.IntervalPeriod intervalPeriod)
    {
        if (intervalPeriod != Models.IntervalPeriod.FifteenMinute && intervalPeriod != Models.IntervalPeriod.ThirtyMinute)
        {
            throw new ArgumentException($"Invalid interval period: {intervalPeriod}", nameof(intervalPeriod));
        }

        int totalMinutes = dateTime.Hour * 60 + dateTime.Minute;
        int periodMinutes = (int)intervalPeriod;
        int periodNumber = (totalMinutes / periodMinutes) + 1;

        return periodNumber;
    }

    /// <summary>
    /// Gets the start time of a given period.
    /// </summary>
    /// <param name="date">The date.</param>
    /// <param name="periodNumber">The period number (1-based).</param>
    /// <param name="intervalPeriod">The interval period.</param>
    /// <returns>The start time of the period.</returns>
    public static DateTime GetPeriodStartTime(DateTime date, int periodNumber, Models.IntervalPeriod intervalPeriod)
    {
        int periodMinutes = (int)intervalPeriod;
        int totalMinutes = (periodNumber - 1) * periodMinutes;
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        return new DateTime(date.Year, date.Month, date.Day, hours, minutes, 0);
    }

    /// <summary>
    /// Gets the end time of a given period.
    /// </summary>
    /// <param name="date">The date.</param>
    /// <param name="periodNumber">The period number (1-based).</param>
    /// <param name="intervalPeriod">The interval period.</param>
    /// <returns>The end time of the period.</returns>
    public static DateTime GetPeriodEndTime(DateTime date, int periodNumber, Models.IntervalPeriod intervalPeriod)
    {
        int periodMinutes = (int)intervalPeriod;
        int totalMinutes = periodNumber * periodMinutes;
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;

        return new DateTime(date.Year, date.Month, date.Day, hours, minutes, 0);
    }

    /// <summary>
    /// Gets the total number of periods in a day for the given interval period.
    /// </summary>
    /// <param name="intervalPeriod">The interval period.</param>
    /// <returns>Number of periods per day (48 for 30-min, 96 for 15-min).</returns>
    public static int GetPeriodsPerDay(Models.IntervalPeriod intervalPeriod)
    {
        return intervalPeriod switch
        {
            Models.IntervalPeriod.FifteenMinute => 96,
            Models.IntervalPeriod.ThirtyMinute => 48,
            _ => throw new ArgumentException($"Invalid interval period: {intervalPeriod}", nameof(intervalPeriod))
        };
    }

    /// <summary>
    /// Validates that a period number is valid for the given interval period.
    /// </summary>
    /// <param name="periodNumber">The period number to validate.</param>
    /// <param name="intervalPeriod">The interval period.</param>
    /// <returns>True if valid; false otherwise.</returns>
    public static bool IsValidPeriod(int periodNumber, Models.IntervalPeriod intervalPeriod)
    {
        int maxPeriods = GetPeriodsPerDay(intervalPeriod);
        return periodNumber >= 1 && periodNumber <= maxPeriods;
    }
}
