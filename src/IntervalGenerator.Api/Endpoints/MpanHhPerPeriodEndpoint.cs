using System.Text.Json;
using System.Text.Json.Serialization;
using IntervalGenerator.Api.Data;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Core.Models;

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

        // Check if MPAN exists
        if (!store.MpanExists(mpan))
        {
            return Results.NotFound(new ErrorResponse
            {
                Error = "Not Found",
                Message = $"MPAN {mpan} not found",
                Status = 404
            });
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
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
                Site = siteName,
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
                    periodDict[reading.Period.ToString()] = new PeriodData
                    {
                        Period = reading.Period,
                        Hhc = reading.ConsumptionKwh,
                        Aei = MapQualityFlag(reading.QualityFlag),
                        QtyId = reading.UnitId
                    };
                }

                dateDict[dateGroup.Key.ToString("yyyy-MM-dd")] = periodDict;
            }

            mcDict[classGroup.Key] = dateDict;
        }

        return new HhPerPeriodResponse
        {
            MPAN = mpan,
            Site = siteName,
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
/// </summary>
public class HhPerPeriodResponse
{
    public required string MPAN { get; set; }

    [JsonPropertyName("site")]
    public required string Site { get; set; }

    public required Dictionary<string, Dictionary<string, Dictionary<string, PeriodData>>> MC { get; set; }
}

/// <summary>
/// Period data for HHPerPeriod response.
/// </summary>
public class PeriodData
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
