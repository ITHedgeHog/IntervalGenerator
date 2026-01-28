using System.Text.Json;
using IntervalGenerator.Api.Data;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Core.Models;
using Microsoft.Extensions.Options;

namespace IntervalGenerator.Api.Endpoints;

/// <summary>
/// Endpoint implementation for /v2/mpanhhperperiod.
/// Returns half-hourly consumption data organized by measurement class and period.
/// </summary>
public static class MpanHhPerPeriodEndpoint
{
    public static void MapMpanHhPerPeriodEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v2/mpanhhperperiod", HandleRequest)
            .WithName("GetMpanHhPerPeriod")
            .WithTags("Consumption Data")
            .WithDescription("Retrieve half-hourly consumption data organized by measurement class and period")
            .Produces<HhPerPeriodResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    private static IResult HandleRequest(
        string mpan,
        IMeterDataStore store,
        IOptions<ApiSettings> settings,
        HttpContext context)
    {
        // Validate MPAN parameter
        if (string.IsNullOrWhiteSpace(mpan))
        {
            return Results.BadRequest(new ErrorResponse
            {
                Error = "Bad Request",
                Message = "MPAN parameter is required",
                Status = 400
            });
        }

        // Check if MPAN exists, and generate if enabled
        if (!store.MpanExists(mpan))
        {
            if (!settings.Value.MeterGeneration.EnableDynamicGeneration)
            {
                return Results.NotFound(new ErrorResponse
                {
                    Error = "Not Found",
                    Message = $"MPAN {mpan} not found",
                    Status = 404
                });
            }

            // Generate 3 years of data (last 3 years from today)
            var endDate = DateTime.Now.Date.AddDays(1).AddTicks(-1); // End of today
            var startDate = endDate.AddYears(-3);

            store.GenerateAndStoreMpan(mpan, startDate, endDate);
        }

        // Get readings and meter details
        var readings = store.GetReadings(mpan).ToList();
        var details = store.GetMeterDetails(mpan);

        // Check response type from header
        var responseType = context.Request.Headers["response-type"].FirstOrDefault() ?? "json";

        if (responseType.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            return HandleCsvResponse(readings, details?.SiteName ?? "");
        }

        // Default to JSON
        return HandleJsonResponse(readings, details?.SiteName ?? "");
    }

    private static IResult HandleJsonResponse(List<IntervalReading> readings, string siteName)
    {
        var response = BuildHhPerPeriodResponse(readings, siteName);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null
        };

        return Results.Json(response, options);
    }

    private static IResult HandleCsvResponse(List<IntervalReading> readings, string siteName)
    {
        var csvContent = BuildCsvContent(readings, siteName);
        return Results.Text(csvContent, "text/csv");
    }

    private static HhPerPeriodResponse BuildHhPerPeriodResponse(List<IntervalReading> readings, string siteName)
    {
        if (readings.Count == 0)
        {
            return new HhPerPeriodResponse
            {
                MPAN = "",
                MC = new Dictionary<string, Dictionary<string, Dictionary<string, PeriodData>>>()
            };
        }

        var mpan = readings[0].Mpan;
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
                        HHC = reading.ConsumptionKwh.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        AEI = MapQualityFlag(reading.QualityFlag)
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

                dateDict[dateGroup.Key.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)] = periodDict;
            }

            mcDict[classGroup.Key] = dateDict;
        }

        return new HhPerPeriodResponse
        {
            MPAN = mpan,
            MC = mcDict
        };
    }

    private static string BuildCsvContent(List<IntervalReading> readings, string siteName)
    {
        var lines = new List<string>
        {
            "MPAN,Site,MeasurementClass,Date,Period,HHC,AEI,QtyId"
        };

        foreach (var reading in readings.OrderBy(r => r.Timestamp).ThenBy(r => r.Period))
        {
            lines.Add($"{reading.Mpan},{siteName},{reading.MeasurementClass},{reading.Timestamp:yyyy-MM-dd},{reading.Period},{reading.ConsumptionKwh:F2},{MapQualityFlag(reading.QualityFlag)},{reading.UnitId}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string MapQualityFlag(DataQuality flag)
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

/// <summary>
/// Response model for HHPerPeriod endpoint.
/// Matches Electralink API format.
/// </summary>
public class HhPerPeriodResponse
{
    public required string MPAN { get; set; }

    public required Dictionary<string, Dictionary<string, Dictionary<string, PeriodData>>> MC { get; set; }
}

/// <summary>
/// Period data for HHPerPeriod response.
/// Matches Electralink API format with uppercase field names and string values.
/// </summary>
public class PeriodData
{
    public string? HHC { get; set; }

    public string? AEI { get; set; }
}
