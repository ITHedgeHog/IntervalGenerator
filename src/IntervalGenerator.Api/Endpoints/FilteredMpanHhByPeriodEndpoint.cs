using System.Text.Json;
using System.Text.Json.Serialization;
using IntervalGenerator.Api.Data;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Core.Models;

namespace IntervalGenerator.Api.Endpoints;

/// <summary>
/// Endpoint implementation for /v1/filteredmpanhhbyperiod.
/// Returns filtered half-hourly data by date range and measurement class.
/// </summary>
public static class FilteredMpanHhByPeriodEndpoint
{
    public static void MapFilteredMpanHhByPeriodEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v1/filteredmpanhhbyperiod", HandleRequest)
            .WithName("GetFilteredMpanHhByPeriod")
            .WithTags("Consumption Data")
            .WithDescription("Retrieve filtered half-hourly data by date range and measurement class")
            .Produces<YearlyHhByPeriodResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);
    }

    private static IResult HandleRequest(
        string mpan,
        string? StartDate,
        string? EndDate,
        string? MeasurementClass,
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

        // Parse date range
        DateOnly? startDate = null;
        DateOnly? endDate = null;

        if (!string.IsNullOrWhiteSpace(StartDate) && DateOnly.TryParse(StartDate, out var parsedStart))
        {
            startDate = parsedStart;
        }

        if (!string.IsNullOrWhiteSpace(EndDate) && DateOnly.TryParse(EndDate, out var parsedEnd))
        {
            endDate = parsedEnd;
        }

        // Parse measurement class filter
        MeasurementClass? measurementClassFilter = null;
        if (!string.IsNullOrWhiteSpace(MeasurementClass))
        {
            // Handle combined measurement class codes like "AIAE" or "AIAERI"
            if (MeasurementClass.StartsWith("AI", StringComparison.OrdinalIgnoreCase))
            {
                measurementClassFilter = Core.Models.MeasurementClass.AI;
            }
            else if (Enum.TryParse<MeasurementClass>(MeasurementClass, true, out var parsed))
            {
                measurementClassFilter = parsed;
            }
        }

        // Get filtered readings
        var readings = store.GetReadings(mpan, startDate, endDate, measurementClassFilter).ToList();
        var details = store.GetMeterDetails(mpan);

        // Build response
        var response = BuildResponse(readings, mpan, startDate, endDate);

        // Check response type from header
        var responseType = context.Request.Headers["response-type"].FirstOrDefault() ?? "json";

        if (responseType.Equals("csv", StringComparison.OrdinalIgnoreCase))
        {
            return HandleCsvResponse(readings, details?.SiteName ?? "");
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return Results.Json(response, options);
    }

    private static YearlyHhByPeriodResponse BuildResponse(
        List<IntervalReading> readings,
        string mpan,
        DateOnly? startDate,
        DateOnly? endDate)
    {
        // Determine actual date range from readings if not specified
        var actualStartDate = startDate ?? (readings.Count > 0 ? DateOnly.FromDateTime(readings.Min(r => r.Timestamp)) : DateOnly.FromDateTime(DateTime.Today));
        var actualEndDate = endDate ?? (readings.Count > 0 ? DateOnly.FromDateTime(readings.Max(r => r.Timestamp)) : DateOnly.FromDateTime(DateTime.Today));

        // Group readings by quality flag
        var actualReadings = readings.Where(r => r.QualityFlag == DataQualityFlag.Actual).ToList();
        var estimatedReadings = readings.Where(r => r.QualityFlag == DataQualityFlag.Estimated).ToList();
        var missingReadings = readings.Where(r => r.QualityFlag == DataQualityFlag.Missing).ToList();

        // Count unique days
        var daysActual = actualReadings.Select(r => r.Timestamp.Date).Distinct().Count();
        var daysEstimated = estimatedReadings.Select(r => r.Timestamp.Date).Distinct().Count();
        var daysMissing = missingReadings.Select(r => r.Timestamp.Date).Distinct().Count();

        // Calculate yearly values
        var aiYearly = readings.Where(r => r.MeasurementClass == Core.Models.MeasurementClass.AI).Sum(r => r.ConsumptionKwh);

        return new YearlyHhByPeriodResponse
        {
            Mpan = mpan,
            StartDate = actualStartDate.ToString("yyyy-MM-dd"),
            EndDate = actualEndDate.ToString("yyyy-MM-dd"),
            DaysActual = daysActual,
            DaysEstimated = daysEstimated,
            DaysMissing = daysMissing,
            AiYearlyValue = aiYearly > 0 ? aiYearly : null,
            AeYearlyValue = null,
            RiYearlyValue = null,
            ReYearlyValue = null,
            ActualMeasurements = BuildDayMeasurements(actualReadings),
            EstimatedMeasurements = BuildDayMeasurements(estimatedReadings),
            MissingMeasurement = BuildDayMeasurements(missingReadings)
        };
    }

    private static List<DayMeasurement> BuildDayMeasurements(List<IntervalReading> readings)
    {
        return readings
            .GroupBy(r => r.Timestamp.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DayMeasurement
            {
                Date = g.Key.ToString("yyyy-MM-dd"),
                QtyId = "kWh",
                Periods = g
                    .OrderBy(r => r.Period)
                    .Select(r => new PeriodMeasurement
                    {
                        Period = r.Period,
                        Hhc = r.ConsumptionKwh,
                        Aei = MapQualityFlag(r.QualityFlag)
                    })
                    .ToList()
            })
            .ToList();
    }

    private static string MapQualityFlag(DataQualityFlag flag)
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

    private static IResult HandleCsvResponse(List<IntervalReading> readings, string siteName)
    {
        var lines = new List<string>
        {
            "MPAN,Date,Period,HHC,AEI,QtyId"
        };

        foreach (var reading in readings.OrderBy(r => r.Timestamp).ThenBy(r => r.Period))
        {
            lines.Add($"{reading.Mpan},{reading.Timestamp:yyyy-MM-dd},{reading.Period},{reading.ConsumptionKwh:F2},{MapQualityFlag(reading.QualityFlag)},{reading.UnitId}");
        }

        return Results.Text(string.Join(Environment.NewLine, lines), "text/csv");
    }
}
