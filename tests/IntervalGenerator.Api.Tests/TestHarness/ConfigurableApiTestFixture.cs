using IntervalGenerator.Api.Data;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Xunit;

namespace IntervalGenerator.Api.Tests.TestHarness;

/// <summary>
/// Configurable test fixture that creates a test server with pre-generated meters using a specified interval period.
/// </summary>
public class ConfigurableApiTestFixture : WebApplicationFactory<Program>
{
    public const int TestMeterCount = 10; // Smaller count for interval tests
    public const string TestApiKey = "test-api-key";
    public const string TestApiPassword = "test-api-password";

    private readonly int _intervalPeriod;
    private readonly string _intervalName;

    public ConfigurableApiTestFixture(int intervalPeriod, string intervalName)
    {
        _intervalPeriod = intervalPeriod;
        _intervalName = intervalName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                ["ApiSettings:Authentication:Enabled"] = "true",
                ["ApiSettings:Authentication:ApiKey"] = TestApiKey,
                ["ApiSettings:Authentication:ApiPassword"] = TestApiPassword,
                ["ApiSettings:MeterGeneration:DefaultMeterCount"] = TestMeterCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["ApiSettings:MeterGeneration:DefaultStartDate"] = "2024-01-01",
                ["ApiSettings:MeterGeneration:DefaultEndDate"] = "2024-01-07", // 1 week for faster tests
                ["ApiSettings:MeterGeneration:DefaultIntervalPeriod"] = _intervalPeriod.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["ApiSettings:MeterGeneration:DeterministicMode"] = "true",
                ["ApiSettings:MeterGeneration:Seed"] = "42"
            };

            config.AddInMemoryCollection(testConfig);
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);
        client.DefaultRequestHeaders.Add("X-Api-Password", TestApiPassword);
        return client;
    }

    public async Task<List<string>> GetAllMpansAsync()
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/mpans");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<MpanListResponse>();
        return content?.Mpans ?? [];
    }

    public int GetExpectedPeriodsPerDay() => _intervalPeriod switch
    {
        5 => 288,
        15 => 96,
        30 => 48,
        _ => throw new ArgumentException($"Unknown interval period: {_intervalPeriod}")
    };

    public string IntervalName => _intervalName;
}

/// <summary>
/// Test fixture for 5-minute interval tests.
/// </summary>
public class FiveMinuteApiTestFixture : ConfigurableApiTestFixture
{
    public FiveMinuteApiTestFixture() : base(5, "5-minute") { }
}

/// <summary>
/// Test fixture for 15-minute interval tests.
/// </summary>
public class FifteenMinuteApiTestFixture : ConfigurableApiTestFixture
{
    public FifteenMinuteApiTestFixture() : base(15, "15-minute") { }
}

/// <summary>
/// Test fixture for 30-minute interval tests.
/// </summary>
public class ThirtyMinuteApiTestFixture : ConfigurableApiTestFixture
{
    public ThirtyMinuteApiTestFixture() : base(30, "30-minute") { }
}
