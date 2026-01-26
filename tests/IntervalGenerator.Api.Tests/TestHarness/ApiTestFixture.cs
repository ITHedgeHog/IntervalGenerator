using IntervalGenerator.Api.Data;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace IntervalGenerator.Api.Tests.TestHarness;

/// <summary>
/// Test fixture that creates a test server with 100 pre-generated meters.
/// </summary>
public class ApiTestFixture : WebApplicationFactory<Program>
{
    public const int TestMeterCount = 100;
    public const string TestApiKey = "test-api-key";
    public const string TestApiPassword = "test-api-password";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Override configuration for testing
            var testConfig = new Dictionary<string, string?>
            {
                ["ApiSettings:Authentication:Enabled"] = "true",
                ["ApiSettings:Authentication:ApiKey"] = TestApiKey,
                ["ApiSettings:Authentication:ApiPassword"] = TestApiPassword,
                ["ApiSettings:MeterGeneration:DefaultMeterCount"] = TestMeterCount.ToString(),
                ["ApiSettings:MeterGeneration:DefaultStartDate"] = "2024-01-01",
                ["ApiSettings:MeterGeneration:DefaultEndDate"] = "2024-01-31", // 1 month for faster tests
                ["ApiSettings:MeterGeneration:DefaultIntervalPeriod"] = "30",
                ["ApiSettings:MeterGeneration:DeterministicMode"] = "true",
                ["ApiSettings:MeterGeneration:Seed"] = "42"
            };

            config.AddInMemoryCollection(testConfig);
        });
    }

    /// <summary>
    /// Creates an HttpClient with authentication headers pre-configured.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", TestApiKey);
        client.DefaultRequestHeaders.Add("X-Api-Password", TestApiPassword);
        return client;
    }

    /// <summary>
    /// Creates an HttpClient without authentication headers.
    /// </summary>
    public HttpClient CreateUnauthenticatedClient()
    {
        return CreateClient();
    }

    /// <summary>
    /// Gets all MPANs from the test server.
    /// </summary>
    public async Task<List<string>> GetAllMpansAsync()
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/mpans");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<MpanListResponse>();
        return content?.Mpans ?? [];
    }
}

/// <summary>
/// Response model for /mpans endpoint.
/// </summary>
public class MpanListResponse
{
    public int Count { get; set; }
    public List<string> Mpans { get; set; } = [];
}
