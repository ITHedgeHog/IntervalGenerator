namespace IntervalGenerator.Core.Models;

/// <summary>
/// Represents the measurement class for energy consumption/generation.
/// Aligns with Electralink EAC API measurement classes.
/// </summary>
public enum MeasurementClass
{
    /// <summary>
    /// Active Import - consumption of electricity.
    /// </summary>
    AI,

    /// <summary>
    /// Active Export - generation/export of electricity.
    /// </summary>
    AE,

    /// <summary>
    /// Reactive Import - reactive power consumption.
    /// </summary>
    RI,

    /// <summary>
    /// Reactive Export - reactive power generation.
    /// </summary>
    RE
}
