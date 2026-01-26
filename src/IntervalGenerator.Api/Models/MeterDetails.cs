namespace IntervalGenerator.Api.Models;

/// <summary>
/// Contains detailed information about a meter for the EacAdditionalDetails endpoint.
/// </summary>
public record MeterDetails
{
    public required string Mpan { get; init; }
    public required Guid MeterId { get; init; }
    public required string SiteName { get; init; }
    public required string BusinessType { get; init; }
    public string Capacity { get; init; } = "100";
    public string EnergisationStatus { get; init; } = "Energised";
    public DateTime EnergisationEffectiveDate { get; init; } = new(2020, 1, 1);
    public required MeterAddress Address { get; init; }
    public string SupplierId { get; init; } = "SUPPLIER001";
    public string LineLossFactorClassId { get; init; } = "EA";
    public DateTime LineLossFactorEffectiveDate { get; init; } = new(2020, 1, 1);
    public string SettlementConfigId { get; init; } = "401";
    public DateTime SettlementConfigEffectiveDate { get; init; } = new(2020, 1, 1);
    public bool IsDisconnected { get; init; }
    public DateTime? DisconnectionDate { get; init; }
    public string MeasurementClassId { get; init; } = "AI";
    public string AssetProviderId { get; init; } = "PROVIDER001";
}

/// <summary>
/// Address information for a meter.
/// </summary>
public record MeterAddress
{
    public string Line1 { get; init; } = string.Empty;
    public string Line2 { get; init; } = string.Empty;
    public string Line3 { get; init; } = string.Empty;
    public string Line4 { get; init; } = string.Empty;
    public string Line5 { get; init; } = string.Empty;
    public string Line6 { get; init; } = string.Empty;
    public string Line7 { get; init; } = string.Empty;
    public string Line8 { get; init; } = string.Empty;
    public string Line9 { get; init; } = string.Empty;
    public string PostCode { get; init; } = string.Empty;
}
