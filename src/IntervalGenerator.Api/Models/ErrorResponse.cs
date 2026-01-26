using System.Text.Json.Serialization;

namespace IntervalGenerator.Api.Models;

/// <summary>
/// Standard error response matching Electralink's error schema.
/// </summary>
public class ErrorResponse
{
    [JsonPropertyName("error")]
    public required string Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }
}
