namespace IntervalGenerator.Core.Models;

/// <summary>
/// Represents the interval period for meter readings.
/// </summary>
public enum IntervalPeriod
{
    /// <summary>
    /// 15-minute intervals (96 periods per day).
    /// </summary>
    FifteenMinute = 15,

    /// <summary>
    /// 30-minute intervals (48 periods per day).
    /// </summary>
    ThirtyMinute = 30
}
