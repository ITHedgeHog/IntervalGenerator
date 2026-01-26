namespace IntervalGenerator.Core.Models;

/// <summary>
/// Represents the quality/status flag for interval readings.
/// Maps to the 'aei' field in Electralink API responses.
/// </summary>
public enum DataQuality
{
    /// <summary>
    /// Actual measured data.
    /// </summary>
    Actual,

    /// <summary>
    /// Estimated data.
    /// </summary>
    Estimated,

    /// <summary>
    /// Missing or not recorded.
    /// </summary>
    Missing,

    /// <summary>
    /// Corrected data.
    /// </summary>
    Corrected
}
