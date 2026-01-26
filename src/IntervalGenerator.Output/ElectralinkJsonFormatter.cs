using System.Text.Json;
using System.Text.Json.Serialization;
using IntervalGenerator.Core.Models;

namespace IntervalGenerator.Output;

/// <summary>
/// Outputs interval readings in Electralink HHPerPeriod JSON format.
/// </summary>
/// <remarks>
/// Produces nested JSON structure:
/// {
///   "MPAN": "1266448934017",
///   "site": "Site Name",
///   "MC": {
///     "AI": {
///       "2024-01-01": {
///         "1": { "period": 1, "hhc": 2.5, "aei": "A", "qty_id": "kWh" }
///       }
///     }
///   }
/// }
/// </remarks>
public sealed class ElectralinkJsonFormatter : IOutputFormatter
{
    /// <inheritdoc />
    public string FormatName => "json";

    /// <inheritdoc />
    public string FileExtension => ".json";

    /// <inheritdoc />
    public async Task WriteAsync(
        IEnumerable<IntervalReading> readings,
        Stream stream,
        OutputOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new OutputOptions();

        var output = BuildHhPerPeriodStructure(readings, options.SiteName);

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = options.PrettyPrint,
            PropertyNamingPolicy = null, // Keep exact property names
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        await JsonSerializer.SerializeAsync(stream, output, jsonOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteToFileAsync(
        IEnumerable<IntervalReading> readings,
        string filePath,
        OutputOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await WriteAsync(readings, stream, options, cancellationToken);
    }

    /// <summary>
    /// Builds the HHPerPeriod structure for multiple meters.
    /// Returns an array if multiple MPANs, or single object if one MPAN.
    /// </summary>
    private static object BuildHhPerPeriodStructure(IEnumerable<IntervalReading> readings, string? siteName)
    {
        // Group readings by MPAN
        var byMpan = readings.GroupBy(r => r.Mpan);
        var meterOutputs = new List<HhPerPeriodOutput>();

        foreach (var mpanGroup in byMpan)
        {
            var output = new HhPerPeriodOutput
            {
                MPAN = mpanGroup.Key,
                Site = siteName ?? "",
                MC = BuildMeasurementClassStructure(mpanGroup)
            };
            meterOutputs.Add(output);
        }

        // Return single object if one meter, array if multiple
        return meterOutputs.Count == 1 ? meterOutputs[0] : meterOutputs;
    }

    /// <summary>
    /// Builds the nested MC (Measurement Class) structure.
    /// </summary>
    private static Dictionary<string, Dictionary<string, Dictionary<string, PeriodData>>> BuildMeasurementClassStructure(
        IEnumerable<IntervalReading> readings)
    {
        var mcDict = new Dictionary<string, Dictionary<string, Dictionary<string, PeriodData>>>();

        // Group by measurement class, then date, then period
        var byClass = readings.GroupBy(r => r.MeasurementClass.ToString());

        foreach (var classGroup in byClass)
        {
            var dateDict = new Dictionary<string, Dictionary<string, PeriodData>>();

            var byDate = classGroup.GroupBy(r => r.Timestamp.Date);

            foreach (var dateGroup in byDate)
            {
                var periodDict = new Dictionary<string, PeriodData>();

                foreach (var reading in dateGroup.OrderBy(r => r.Period))
                {
                    periodDict[reading.Period.ToString()] = new PeriodData
                    {
                        Period = reading.Period,
                        Hhc = reading.ConsumptionKwh,
                        Aei = MapQualityFlagToAei(reading.QualityFlag),
                        QtyId = reading.UnitId
                    };
                }

                dateDict[dateGroup.Key.ToString("yyyy-MM-dd")] = periodDict;
            }

            mcDict[classGroup.Key] = dateDict;
        }

        return mcDict;
    }

    private static string MapQualityFlagToAei(DataQualityFlag flag)
    {
        return flag switch
        {
            DataQualityFlag.Actual => "A",
            DataQualityFlag.Estimated => "E",
            DataQualityFlag.Missing => "M",
            DataQualityFlag.Corrected => "X",
            _ => "A"
        };
    }

    #region JSON Output Models

    private class HhPerPeriodOutput
    {
        public required string MPAN { get; set; }

        [JsonPropertyName("site")]
        public required string Site { get; set; }

        public required Dictionary<string, Dictionary<string, Dictionary<string, PeriodData>>> MC { get; set; }
    }

    private class PeriodData
    {
        [JsonPropertyName("period")]
        public int Period { get; set; }

        [JsonPropertyName("hhc")]
        public decimal Hhc { get; set; }

        [JsonPropertyName("aei")]
        public required string Aei { get; set; }

        [JsonPropertyName("qty_id")]
        public required string QtyId { get; set; }
    }

    #endregion
}
