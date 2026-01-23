namespace IntervalGenerator.Core.Models;

/// <summary>
/// Represents a single half-hourly interval reading from a meter.
/// </summary>
public record IntervalReading
{
    /// <summary>
    /// Gets the unique identifier for the meter (internal GUID).
    /// </summary>
    public required Guid MeterId { get; init; }

    /// <summary>
    /// Gets the MPAN (13-digit meter identifier) derived from the MeterId.
    /// Used in Electralink API responses.
    /// </summary>
    public required string Mpan { get; init; }

    /// <summary>
    /// Gets the timestamp for the start of the interval (UTC).
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the interval period number (1-48 for 30-min, 1-96 for 15-min).
    /// </summary>
    public required int Period { get; init; }

    /// <summary>
    /// Gets the consumption value in kWh with 2 decimal places.
    /// Maps to the 'hhc' field in Electralink API.
    /// </summary>
    public required decimal ConsumptionKwh { get; init; }

    /// <summary>
    /// Gets the measurement class (AI, AE, RI, RE).
    /// </summary>
    public required MeasurementClass MeasurementClass { get; init; }

    /// <summary>
    /// Gets the data quality flag (Actual, Estimated, Missing, Corrected).
    /// Maps to the 'aei' field in Electralink API.
    /// </summary>
    public DataQualityFlag QualityFlag { get; init; } = DataQualityFlag.Actual;

    /// <summary>
    /// Gets the business type this meter belongs to.
    /// </summary>
    public required string BusinessType { get; init; }

    /// <summary>
    /// Gets the unit identifier (e.g., "kWh").
    /// Maps to the 'qty_id' field in Electralink API.
    /// </summary>
    public string UnitId { get; init; } = "kWh";
}
