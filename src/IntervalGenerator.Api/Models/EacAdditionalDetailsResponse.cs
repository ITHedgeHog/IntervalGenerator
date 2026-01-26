using System.Text.Json.Serialization;

namespace IntervalGenerator.Api.Models;

/// <summary>
/// Response model for /v2/mpanadditionaldetails endpoint.
/// Matches Electralink's EacAdditionalDetailsV2 schema exactly.
/// </summary>
public class EacAdditionalDetailsResponse
{
    [JsonPropertyName("mpan")]
    public required string Mpan { get; set; }

    [JsonPropertyName("capacity")]
    public required string Capacity { get; set; }

    [JsonPropertyName("energisation_status")]
    public required string EnergisationStatus { get; set; }

    [JsonPropertyName("energisation_status_effective_from_date")]
    public required string EnergisationStatusEffectiveFromDate { get; set; }

    [JsonPropertyName("metering_point_address_line_1")]
    public required string MeteringPointAddressLine1 { get; set; }

    [JsonPropertyName("metering_point_address_line_2")]
    public required string MeteringPointAddressLine2 { get; set; }

    [JsonPropertyName("metering_point_address_line_3")]
    public required string MeteringPointAddressLine3 { get; set; }

    [JsonPropertyName("metering_point_address_line_4")]
    public required string MeteringPointAddressLine4 { get; set; }

    [JsonPropertyName("metering_point_address_line_5")]
    public required string MeteringPointAddressLine5 { get; set; }

    [JsonPropertyName("metering_point_address_line_6")]
    public required string MeteringPointAddressLine6 { get; set; }

    [JsonPropertyName("metering_point_address_line_7")]
    public required string MeteringPointAddressLine7 { get; set; }

    [JsonPropertyName("metering_point_address_line_8")]
    public required string MeteringPointAddressLine8 { get; set; }

    [JsonPropertyName("metering_point_address_line_9")]
    public required string MeteringPointAddressLine9 { get; set; }

    [JsonPropertyName("post_code")]
    public required string PostCode { get; set; }

    [JsonPropertyName("supplier_id")]
    public required string SupplierId { get; set; }

    [JsonPropertyName("line_loss_factor_class_id")]
    public required string LineLossFactorClassId { get; set; }

    [JsonPropertyName("line_loss_factor_class_id_effective_from_date")]
    public required string LineLossFactorClassIdEffectiveFromDate { get; set; }

    [JsonPropertyName("standard_settlement_configuration_id")]
    public required string StandardSettlementConfigurationId { get; set; }

    [JsonPropertyName("standard_settlement_configuration_id_effective_from_date")]
    public required string StandardSettlementConfigurationIdEffectiveFromDate { get; set; }

    [JsonPropertyName("disconnected_mpan")]
    public required string DisconnectedMpan { get; set; }

    [JsonPropertyName("disconnection_date")]
    public string? DisconnectionDate { get; set; }

    [JsonPropertyName("additional_detail")]
    public required List<AdditionalDetailItem> AdditionalDetail { get; set; }
}

/// <summary>
/// Additional detail item for a meter.
/// </summary>
public class AdditionalDetailItem
{
    [JsonPropertyName("meter_id")]
    public required string MeterId { get; set; }

    [JsonPropertyName("measurement_class_id")]
    public required string MeasurementClassId { get; set; }

    [JsonPropertyName("asset_provider_id")]
    public required string AssetProviderId { get; set; }
}
