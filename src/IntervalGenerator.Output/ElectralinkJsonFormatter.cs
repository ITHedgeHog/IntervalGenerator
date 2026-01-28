using System.Globalization;
using System.Text.Json;
using IntervalGenerator.Core.Models;

namespace IntervalGenerator.Output;

/// <summary>
/// Outputs interval readings in Electralink HHPerPeriod JSON format.
/// </summary>
/// <remarks>
/// Produces nested JSON structure:
/// {
///   "MPAN": "1266448934017",
///   "MC": {
///     "AI": {
///       "2024-01-01": {
///         "P1": { "HHC": "2.5", "AEI": "A" },
///         "P49": { "HHC": null, "AEI": null },
///         "P50": { "HHC": null, "AEI": null }
///       }
///     }
///   }
/// }
/// </remarks>
public sealed class ElectralinkJsonFormatter : IOutputFormatter
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null
    };

    private static readonly JsonSerializerOptions PrettyPrintOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

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
        var jsonOptions = options.PrettyPrint ? PrettyPrintOptions : DefaultOptions;

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
                    var periodKey = $"P{reading.Period}";
                    periodDict[periodKey] = new PeriodData
                    {
                        HHC = reading.ConsumptionKwh.ToString(CultureInfo.InvariantCulture),
                        AEI = MapQualityFlagToAei(reading.QualityFlag)
                    };
                }

                // Add P49 and P50 with null values for 30-minute intervals (48 periods)
                // Electralink format includes P49/P50 padding for 30-minute interval data
                var maxPeriod = dateGroup.Max(r => r.Period);
                if (maxPeriod <= 48)
                {
                    periodDict["P49"] = new PeriodData { HHC = null, AEI = null };
                    periodDict["P50"] = new PeriodData { HHC = null, AEI = null };
                }

                dateDict[dateGroup.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)] = periodDict;
            }

            mcDict[classGroup.Key] = dateDict;
        }

        return mcDict;
    }

    private static string MapQualityFlagToAei(DataQuality flag)
    {
        return flag switch
        {
            DataQuality.Actual => "A",
            DataQuality.Estimated => "E",
            DataQuality.Missing => "M",
            DataQuality.Corrected => "X",
            _ => "A"
        };
    }

    #region JSON Output Models

    private sealed class HhPerPeriodOutput
    {
        public required string MPAN { get; set; }

        public required Dictionary<string, Dictionary<string, Dictionary<string, PeriodData>>> MC { get; set; }
    }

    private sealed class PeriodData
    {
        public string? HHC { get; set; }

        public string? AEI { get; set; }
    }

    #endregion
}
