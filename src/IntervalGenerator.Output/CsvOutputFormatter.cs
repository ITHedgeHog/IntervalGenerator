using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using IntervalGenerator.Core.Models;

namespace IntervalGenerator.Output;

/// <summary>
/// Outputs interval readings in Electralink-compatible CSV format.
/// </summary>
/// <remarks>
/// CSV Format:
/// MPAN,Site,MeasurementClass,Date,Period,HHC,AEI,QtyId
/// 1266448934017,Site A,AI,2024-01-01,1,2.5,A,kWh
/// </remarks>
public sealed class CsvOutputFormatter : IOutputFormatter
{
    /// <inheritdoc />
    public string FormatName => "csv";

    /// <inheritdoc />
    public string FileExtension => ".csv";

    /// <inheritdoc />
    public async Task WriteAsync(
        IEnumerable<IntervalReading> readings,
        Stream stream,
        OutputOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new OutputOptions();

        await using var writer = new StreamWriter(stream, leaveOpen: true);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = options.IncludeHeaders
        });

        if (options.IncludeHeaders)
        {
            await WriteHeaderAsync(csv);
            await csv.NextRecordAsync();
        }

        foreach (var reading in readings)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteRecord(csv, reading, options.SiteName);
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync(cancellationToken);
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

    private static async Task WriteHeaderAsync(CsvWriter csv)
    {
        csv.WriteField("MPAN");
        csv.WriteField("Site");
        csv.WriteField("MeasurementClass");
        csv.WriteField("Date");
        csv.WriteField("Period");
        csv.WriteField("HHC");
        csv.WriteField("AEI");
        csv.WriteField("QtyId");
        await Task.CompletedTask;
    }

    private static void WriteRecord(CsvWriter csv, IntervalReading reading, string? siteName)
    {
        csv.WriteField(reading.Mpan);
        csv.WriteField(siteName ?? "");
        csv.WriteField(reading.MeasurementClass.ToString());
        csv.WriteField(reading.Timestamp.ToString("yyyy-MM-dd"));
        csv.WriteField(reading.Period);
        csv.WriteField(reading.ConsumptionKwh);
        csv.WriteField(MapQualityFlagToAei(reading.QualityFlag));
        csv.WriteField(reading.UnitId);
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
}
