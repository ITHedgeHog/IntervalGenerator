namespace IntervalGenerator.Core.Models;

/// <summary>
/// Represents the result of interval generation.
/// </summary>
public record GenerationResult
{
    /// <summary>
    /// Gets the generated interval readings.
    /// </summary>
    public required IReadOnlyList<IntervalReading> Readings { get; init; }

    /// <summary>
    /// Gets the original generation configuration used.
    /// </summary>
    public required GenerationConfiguration Configuration { get; init; }

    /// <summary>
    /// Gets the total number of readings generated.
    /// </summary>
    public int TotalReadings => Readings.Count;

    /// <summary>
    /// Gets the unique meter IDs in the result.
    /// </summary>
    public IReadOnlyList<Guid> UniqueMeterIds => Readings
        .Select(r => r.MeterId)
        .Distinct()
        .ToList();

    /// <summary>
    /// Gets the total consumption across all readings.
    /// </summary>
    public decimal TotalConsumptionKwh => Readings.Sum(r => r.ConsumptionKwh);

    /// <summary>
    /// Gets the minimum consumption value.
    /// </summary>
    public decimal MinConsumptionKwh => Readings.Count > 0 ? Readings.Min(r => r.ConsumptionKwh) : 0;

    /// <summary>
    /// Gets the maximum consumption value.
    /// </summary>
    public decimal MaxConsumptionKwh => Readings.Count > 0 ? Readings.Max(r => r.ConsumptionKwh) : 0;

    /// <summary>
    /// Gets the average consumption value.
    /// </summary>
    public decimal AverageConsumptionKwh => Readings.Count > 0
        ? Readings.Average(r => r.ConsumptionKwh)
        : 0;

    /// <summary>
    /// Gets the generation timestamp (UTC).
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}
