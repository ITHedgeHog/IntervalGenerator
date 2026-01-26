namespace IntervalGenerator.Core.Models;

/// <summary>
/// Configuration for interval generation.
/// </summary>
public record GenerationConfiguration
{
    /// <summary>
    /// Gets the start date for generation (inclusive).
    /// </summary>
    public required DateTime StartDate { get; init; }

    /// <summary>
    /// Gets the end date for generation (inclusive).
    /// </summary>
    public required DateTime EndDate { get; init; }

    /// <summary>
    /// Gets the interval period (15-minute or 30-minute).
    /// </summary>
    public required IntervalPeriod Period { get; init; }

    /// <summary>
    /// Gets the business type to generate data for.
    /// Examples: "Office", "Manufacturing", "Retail"
    /// </summary>
    public required string BusinessType { get; init; }

    /// <summary>
    /// Gets the measurement class to generate.
    /// </summary>
    public MeasurementClass MeasurementClass { get; init; } = MeasurementClass.AI;

    /// <summary>
    /// Gets the number of meters to generate (1-1000).
    /// </summary>
    public int MeterCount { get; init; } = 1;

    /// <summary>
    /// Gets whether generation should be deterministic (seeded).
    /// </summary>
    public bool Deterministic { get; init; }

    /// <summary>
    /// Gets the random seed for deterministic generation.
    /// If null and Deterministic is true, seed will be generated from configuration hash.
    /// </summary>
    public int? Seed { get; init; }

    /// <summary>
    /// Gets optional specific meter IDs to generate.
    /// If null, GUIDs will be auto-generated.
    /// </summary>
    public IReadOnlyList<Guid>? MeterIds { get; init; }

    /// <summary>
    /// Gets the site/location name to include in metadata.
    /// </summary>
    public string? SiteName { get; init; }
}
