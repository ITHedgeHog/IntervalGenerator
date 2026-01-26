namespace IntervalGenerator.Api.Models;

/// <summary>
/// API configuration settings.
/// </summary>
public class ApiSettings
{
    public AuthenticationSettings Authentication { get; set; } = new();
    public MeterGenerationSettings MeterGeneration { get; set; } = new();
}

/// <summary>
/// Authentication settings.
/// </summary>
public class AuthenticationSettings
{
    public bool Enabled { get; set; } = true;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiPassword { get; set; } = string.Empty;
}

/// <summary>
/// Meter generation settings for API startup.
/// </summary>
public class MeterGenerationSettings
{
    public int DefaultMeterCount { get; set; } = 100;
    public string DefaultStartDate { get; set; } = "2024-01-01";
    public string DefaultEndDate { get; set; } = "2024-12-31";
    public int DefaultIntervalPeriod { get; set; } = 30;
    public string DefaultBusinessType { get; set; } = "Office";
    public bool DeterministicMode { get; set; } = true;
    public int Seed { get; set; } = 42;
}
