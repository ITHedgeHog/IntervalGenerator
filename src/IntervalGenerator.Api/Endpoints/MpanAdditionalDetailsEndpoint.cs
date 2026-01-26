using IntervalGenerator.Api.Data;
using IntervalGenerator.Api.Models;

namespace IntervalGenerator.Api.Endpoints;

/// <summary>
/// Endpoint implementation for /v2/mpanadditionaldetails.
/// Returns meter metadata and address information.
/// </summary>
public static class MpanAdditionalDetailsEndpoint
{
    public static void MapMpanAdditionalDetailsEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/v2/mpanadditionaldetails", HandleRequest)
            .WithName("GetMpanAdditionalDetails")
            .WithTags("Meter Details")
            .WithDescription("Retrieve meter metadata and address information")
            .Produces<EacAdditionalDetailsResponse>(StatusCodes.Status200OK)
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

        // Get meter details
        var details = store.GetMeterDetails(mpan);

        if (details == null)
        {
            return Results.NotFound(new ErrorResponse
            {
                Error = "Not Found",
                Message = $"MPAN {mpan} not found",
                Status = 404
            });
        }

        // Build response
        var response = BuildResponse(details);

        // Check response type from header
        var responseType = context.Request.Headers["response-type"].FirstOrDefault()?.ToLower() ?? "json";

        if (responseType == "csv")
        {
            return HandleCsvResponse(response);
        }

        return Results.Ok(response);
    }

    private static EacAdditionalDetailsResponse BuildResponse(MeterDetails details)
    {
        return new EacAdditionalDetailsResponse
        {
            Mpan = details.Mpan,
            Capacity = details.Capacity,
            EnergisationStatus = details.EnergisationStatus,
            EnergisationStatusEffectiveFromDate = details.EnergisationEffectiveDate.ToString("yyyy-MM-dd"),
            MeteringPointAddressLine1 = details.Address.Line1,
            MeteringPointAddressLine2 = details.Address.Line2,
            MeteringPointAddressLine3 = details.Address.Line3,
            MeteringPointAddressLine4 = details.Address.Line4,
            MeteringPointAddressLine5 = details.Address.Line5,
            MeteringPointAddressLine6 = details.Address.Line6,
            MeteringPointAddressLine7 = details.Address.Line7,
            MeteringPointAddressLine8 = details.Address.Line8,
            MeteringPointAddressLine9 = details.Address.Line9,
            PostCode = details.Address.PostCode,
            SupplierId = details.SupplierId,
            LineLossFactorClassId = details.LineLossFactorClassId,
            LineLossFactorClassIdEffectiveFromDate = details.LineLossFactorEffectiveDate.ToString("yyyy-MM-dd"),
            StandardSettlementConfigurationId = details.SettlementConfigId,
            StandardSettlementConfigurationIdEffectiveFromDate = details.SettlementConfigEffectiveDate.ToString("yyyy-MM-dd"),
            DisconnectedMpan = details.IsDisconnected.ToString().ToLower(),
            DisconnectionDate = details.DisconnectionDate?.ToString("yyyy-MM-dd"),
            AdditionalDetail =
            [
                new AdditionalDetailItem
                {
                    MeterId = $"M{details.Mpan[..9]}",
                    MeasurementClassId = details.MeasurementClassId,
                    AssetProviderId = details.AssetProviderId
                }
            ]
        };
    }

    private static IResult HandleCsvResponse(EacAdditionalDetailsResponse response)
    {
        var header = "mpan,capacity,energisation_status,post_code,supplier_id";
        var data = $"{response.Mpan},{response.Capacity},{response.EnergisationStatus},{response.PostCode},{response.SupplierId}";
        var csvContent = $"{header}{Environment.NewLine}{data}";
        return Results.Text(csvContent, "text/csv");
    }
}
