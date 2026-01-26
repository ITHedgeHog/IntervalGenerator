using System.Text.Json.Serialization;

namespace IntervalGenerator.Api.Models;

/// <summary>
/// Response model for /v1/filteredmpanhhbyperiod endpoint.
/// Matches Electralink's YearlyHHByPeriodOutput schema.
/// </summary>
public class YearlyHhByPeriodResponse
{
    [JsonPropertyName("mpan")]
    public required string Mpan { get; set; }

    [JsonPropertyName("start_date")]
    public required string StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public required string EndDate { get; set; }

    [JsonPropertyName("days_actual")]
    public int DaysActual { get; set; }

    [JsonPropertyName("days_estimated")]
    public int DaysEstimated { get; set; }

    [JsonPropertyName("days_missing")]
    public int DaysMissing { get; set; }

    [JsonPropertyName("ai_yearly_value")]
    public decimal? AiYearlyValue { get; set; }

    [JsonPropertyName("ae_yearly_value")]
    public decimal? AeYearlyValue { get; set; }

    [JsonPropertyName("ri_yearly_value")]
    public decimal? RiYearlyValue { get; set; }

    [JsonPropertyName("re_yearly_value")]
    public decimal? ReYearlyValue { get; set; }

    [JsonPropertyName("actual_measurements")]
    public required List<DayMeasurement> ActualMeasurements { get; set; }

    [JsonPropertyName("estimated_measurements")]
    public required List<DayMeasurement> EstimatedMeasurements { get; set; }

    [JsonPropertyName("missing_measurement")]
    public required List<DayMeasurement> MissingMeasurement { get; set; }
}

/// <summary>
/// A day's worth of period measurements.
/// </summary>
public class DayMeasurement
{
    [JsonPropertyName("date")]
    public required string Date { get; set; }

    [JsonPropertyName("qty_id")]
    public required string QtyId { get; set; }

    [JsonPropertyName("periods")]
    public required List<PeriodMeasurement> Periods { get; set; }
}

/// <summary>
/// A single period measurement.
/// </summary>
public class PeriodMeasurement
{
    [JsonPropertyName("period")]
    public int Period { get; set; }

    [JsonPropertyName("hhc")]
    public decimal Hhc { get; set; }

    [JsonPropertyName("aei")]
    public required string Aei { get; set; }
}
