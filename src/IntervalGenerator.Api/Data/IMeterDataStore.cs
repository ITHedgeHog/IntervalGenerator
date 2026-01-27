using IntervalGenerator.Api.Models;
using IntervalGenerator.Core.Models;

namespace IntervalGenerator.Api.Data;

/// <summary>
/// Interface for meter data storage and retrieval.
/// </summary>
public interface IMeterDataStore
{
    /// <summary>
    /// Initialize the store with generated meter data.
    /// </summary>
    Task InitializeAsync(int meterCount, GenerationConfiguration baseConfig);

    /// <summary>
    /// Get all readings for a specific MPAN, optionally filtered by date range.
    /// </summary>
    IEnumerable<IntervalReading> GetReadings(
        string mpan,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        MeasurementClass? measurementClass = null);

    /// <summary>
    /// Get meter details for a specific MPAN.
    /// </summary>
    MeterDetails? GetMeterDetails(string mpan);

    /// <summary>
    /// Get all available MPANs.
    /// </summary>
    IEnumerable<string> GetAllMpans();

    /// <summary>
    /// Check if an MPAN exists in the store.
    /// </summary>
    bool MpanExists(string mpan);

    /// <summary>
    /// Generate and store meter data for a new MPAN.
    /// </summary>
    void GenerateAndStoreMpan(string mpan, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Get the total count of meters in the store.
    /// </summary>
    int MeterCount { get; }

    /// <summary>
    /// Indicates whether the store has been initialized.
    /// </summary>
    bool IsInitialized { get; }
}
