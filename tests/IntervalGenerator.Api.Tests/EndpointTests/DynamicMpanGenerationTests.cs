using System.Net;
using System.Text.Json;
using FluentAssertions;
using IntervalGenerator.Api.Endpoints;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Api.Tests.TestHarness;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IntervalGenerator.Api.Tests.EndpointTests;

/// <summary>
/// Tests for dynamic MPAN generation feature.
/// Verifies that:
/// 1. Non-existent MPANs can be dynamically generated when enabled
/// 2. Generated data covers requested date ranges (e.g., 14 months)
/// 3. Deterministic generation produces identical results for same MPAN
/// </summary>
public class DynamicMpanGenerationTests : IClassFixture<DynamicGenerationTestFixture>
{
    private readonly DynamicGenerationTestFixture _fixture;
    private readonly HttpClient _client;

    public DynamicMpanGenerationTests(DynamicGenerationTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    #region 14-Month Data Coverage Tests

    [Fact]
    public async Task DynamicGeneration_14MonthRange_ReturnsCompleteDataCoverage()
    {
        // Arrange
        var testMpan = "1234567890123"; // Non-existent MPAN
        var endDate = DateTime.Now.Date;
        var startDate = endDate.AddMonths(-14); // 14 months back

        // Act
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate={startDate:yyyy-MM-dd}&EndDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        data.Should().NotBeNull();
        data!.Mpan.Should().Be(testMpan);

        // Verify date range
        var expectedStartDate = DateTime.ParseExact(data.StartDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var expectedEndDate = DateTime.ParseExact(data.EndDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        expectedStartDate.Date.Should().Be(startDate.Date);
        expectedEndDate.Date.Should().Be(endDate.Date);
    }

    [Fact]
    public async Task DynamicGeneration_14MonthRange_HasExpectedReadingCount()
    {
        // Arrange
        var testMpan = "9876543210987"; // Non-existent MPAN
        var endDate = DateTime.Now.Date;
        var startDate = endDate.AddMonths(-14); // 14 months back

        // Act
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate={startDate:yyyy-MM-dd}&EndDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        data.Should().NotBeNull();
        data!.ActualMeasurements.Should().NotBeEmpty();

        // Calculate expected days and periods
        var dayCount = (int)Math.Ceiling((endDate - startDate).TotalDays) + 1;
        var periodsPerDay = 48; // 30-minute intervals = 48 per day
        var expectedTotalPeriods = dayCount * periodsPerDay;

        // Count actual periods from response
        var totalPeriods = data.ActualMeasurements.Sum(d => d.Periods.Count);

        // Should have readings for the requested range
        totalPeriods.Should().BeGreaterThan(0);
        totalPeriods.Should().BeLessThanOrEqualTo(expectedTotalPeriods);
    }

    [Fact]
    public async Task DynamicGeneration_14MonthRange_CoversAllDays()
    {
        // Arrange
        var testMpan = "5555555555555"; // Non-existent MPAN
        var endDate = DateTime.Now.Date;
        var startDate = endDate.AddMonths(-14); // 14 months back

        // Act
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate={startDate:yyyy-MM-dd}&EndDate={endDate:yyyy-MM-dd}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        data.Should().NotBeNull();

        // All actual measurements should be marked as Actual
        var allPeriods = data!.ActualMeasurements.SelectMany(m => m.Periods).ToList();
        allPeriods.Should().NotBeEmpty();
        allPeriods.All(p => p.Aei == "A").Should().BeTrue("All periods in deterministic mode should be marked as Actual");

        // Verify daily count
        var dayCount = (int)Math.Ceiling((endDate - startDate).TotalDays) + 1;
        data.DaysActual.Should().Be(dayCount, $"Should have {dayCount} days of actual data");
    }

    [Fact]
    public async Task DynamicGeneration_14MonthRange_HasValidConsumptionValues()
    {
        // Arrange
        var testMpan = "2222222222222"; // Non-existent MPAN
        var endDate = DateTime.Now.Date;
        var startDate = endDate.AddMonths(-14);

        // Act
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate={startDate:yyyy-MM-dd}&EndDate={endDate:yyyy-MM-dd}");

        // Assert
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();
        data.Should().NotBeNull();

        var allPeriods = data!.ActualMeasurements.SelectMany(m => m.Periods).ToList();
        allPeriods.Should().NotBeEmpty();

        // Most consumption values should be positive (some may be near zero due to profile modifiers)
        var positiveCount = allPeriods.Count(p => p.Hhc > 0);
        var positivePercentage = (decimal)positiveCount / allPeriods.Count;
        positivePercentage.Should().BeGreaterThan(0.75m, "At least 75% of consumption values should be positive due to profile modifiers");

        // Yearly total should be reasonable
        data.AiYearlyValue.Should().BeGreaterThan(0, "Should have total yearly consumption");
    }

    #endregion

    #region Deterministic Generation Tests

    [Fact]
    public async Task DynamicGeneration_SameMpan_ProducesDeterministicResults()
    {
        // Arrange
        var testMpan = "3333333333333"; // Non-existent MPAN
        var startDate = "2023-06-01";
        var endDate = "2023-08-31";

        // Act - Generate twice for the same MPAN
        var response1 = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate={startDate}&EndDate={endDate}");
        var data1 = await response1.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        var response2 = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate={startDate}&EndDate={endDate}");
        var data2 = await response2.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert
        data1.Should().NotBeNull();
        data2.Should().NotBeNull();

        // Verify same structure
        data1!.ActualMeasurements.Count.Should().Be(data2!.ActualMeasurements.Count);
        data1.DaysActual.Should().Be(data2.DaysActual);

        // Verify identical consumption values
        for (int i = 0; i < data1.ActualMeasurements.Count; i++)
        {
            var periods1 = data1.ActualMeasurements[i].Periods.OrderBy(p => p.Period).ToList();
            var periods2 = data2.ActualMeasurements[i].Periods.OrderBy(p => p.Period).ToList();

            periods1.Count.Should().Be(periods2.Count);

            for (int j = 0; j < periods1.Count; j++)
            {
                periods1[j].Hhc.Should().Be(periods2[j].Hhc,
                    $"Period consumption should be identical for deterministic generation");
                periods1[j].Aei.Should().Be(periods2[j].Aei);
            }
        }
    }

    [Fact]
    public async Task DynamicGeneration_DifferentMpans_ProduceDifferentConsumption()
    {
        // Arrange
        var mpan1 = "4444444444444";
        var mpan2 = "6666666666666";
        var startDate = "2023-06-01";
        var endDate = "2023-06-30";

        // Act
        var response1 = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={mpan1}&StartDate={startDate}&EndDate={endDate}");
        var data1 = await response1.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        var response2 = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={mpan2}&StartDate={startDate}&EndDate={endDate}");
        var data2 = await response2.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert
        data1.Should().NotBeNull();
        data2.Should().NotBeNull();

        // Different MPANs should produce different consumption patterns (statistically)
        var periods1 = data1!.ActualMeasurements.SelectMany(m => m.Periods).OrderBy(p => p.Period).ToList();
        var periods2 = data2!.ActualMeasurements.SelectMany(m => m.Periods).OrderBy(p => p.Period).ToList();

        // Calculate total consumption for each
        var total1 = periods1.Sum(p => p.Hhc);
        var total2 = periods2.Sum(p => p.Hhc);

        // Totals should be different (with high probability for different seeds)
        total1.Should().NotBe(total2, "Different MPANs should produce different total consumption");
    }

    [Fact]
    public async Task DynamicGeneration_MpanHhPerPeriod_DeterministicResults()
    {
        // Arrange
        var testMpan = "7777777777777"; // Non-existent MPAN

        // Act - Call twice for the same MPAN
        var response1 = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");
        var data1 = await response1.Content.ReadFromJsonAsync<HhPerPeriodResponse>();

        var response2 = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");
        var data2 = await response2.Content.ReadFromJsonAsync<HhPerPeriodResponse>();

        // Assert
        data1.Should().NotBeNull();
        data2.Should().NotBeNull();

        // Verify same structure
        data1!.MC.Keys.Should().BeEquivalentTo(data2!.MC.Keys);

        // For each measurement class, verify identical consumption
        foreach (var mcKey in data1.MC.Keys)
        {
            data1.MC[mcKey].Keys.Should().BeEquivalentTo(data2.MC[mcKey].Keys);

            foreach (var dateKey in data1.MC[mcKey].Keys)
            {
                var periods1 = data1.MC[mcKey][dateKey].Values.OrderBy(p => p.Period).ToList();
                var periods2 = data2.MC[mcKey][dateKey].Values.OrderBy(p => p.Period).ToList();

                periods1.Count.Should().Be(periods2.Count);

                for (int i = 0; i < periods1.Count; i++)
                {
                    periods1[i].Hhc.Should().Be(periods2[i].Hhc,
                        $"Period consumption should be identical in deterministic mode");
                }
            }
        }
    }

    [Fact]
    public async Task DynamicGeneration_AdditionalDetails_DeterministicResults()
    {
        // Arrange
        var testMpan = "8888888888888"; // Non-existent MPAN

        // Act - Call twice for the same MPAN
        var response1 = await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={testMpan}");
        var data1 = await response1.Content.ReadFromJsonAsync<EacAdditionalDetailsResponse>();

        var response2 = await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={testMpan}");
        var data2 = await response2.Content.ReadFromJsonAsync<EacAdditionalDetailsResponse>();

        // Assert
        data1.Should().NotBeNull();
        data2.Should().NotBeNull();

        // Verify identical metadata
        data1!.Mpan.Should().Be(data2!.Mpan);
        data1.Capacity.Should().Be(data2.Capacity);
        data1.SupplierId.Should().Be(data2.SupplierId);
        data1.MeteringPointAddressLine1.Should().Be(data2.MeteringPointAddressLine1);
        data1.PostCode.Should().Be(data2.PostCode);
    }

    [Fact]
    public async Task DynamicGeneration_SameMpan_RepeatedRequestsReturnSameData()
    {
        // Arrange
        var testMpan = "9999999999999"; // Non-existent MPAN
        var startDate = "2023-01-01";
        var endDate = "2023-01-31";

        // Act - Call filtered endpoint twice for the same date range
        var response1 = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate={startDate}&EndDate={endDate}");
        var data1 = await response1.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        var response2 = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate={startDate}&EndDate={endDate}");
        var data2 = await response2.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert
        data1.Should().NotBeNull();
        data2.Should().NotBeNull();

        // Verify identical structure
        data1!.ActualMeasurements.Count.Should().Be(data2!.ActualMeasurements.Count);
        data1.DaysActual.Should().Be(data2.DaysActual);

        // Verify identical consumption across all periods
        for (int i = 0; i < data1.ActualMeasurements.Count; i++)
        {
            var periods1 = data1.ActualMeasurements[i].Periods.OrderBy(p => p.Period).ToList();
            var periods2 = data2.ActualMeasurements[i].Periods.OrderBy(p => p.Period).ToList();

            periods1.Count.Should().Be(periods2.Count);

            for (int j = 0; j < periods1.Count; j++)
            {
                periods1[j].Hhc.Should().Be(periods2[j].Hhc,
                    "Consumption should be identical for repeated requests of same MPAN/date range");
            }
        }
    }

    #endregion
}

/// <summary>
/// Test fixture that enables dynamic MPAN generation.
/// </summary>
public class DynamicGenerationTestFixture : WebApplicationFactory<Program>
{
    public const string TestApiKey = "test-api-key";
    public const string TestApiPassword = "test-api-password";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                ["ApiSettings:Authentication:Enabled"] = "true",
                ["ApiSettings:Authentication:ApiKey"] = TestApiKey,
                ["ApiSettings:Authentication:ApiPassword"] = TestApiPassword,
                ["ApiSettings:MeterGeneration:DefaultMeterCount"] = "10", // Small count for quick setup
                ["ApiSettings:MeterGeneration:DefaultStartDate"] = "2024-01-01",
                ["ApiSettings:MeterGeneration:DefaultEndDate"] = "2024-01-31",
                ["ApiSettings:MeterGeneration:DefaultIntervalPeriod"] = "30",
                ["ApiSettings:MeterGeneration:DeterministicMode"] = "true",
                ["ApiSettings:MeterGeneration:Seed"] = "42",
                ["ApiSettings:MeterGeneration:EnableDynamicGeneration"] = "true" // Enable dynamic generation
            };

            config.AddInMemoryCollection(testConfig);
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Api-Key", TestApiKey);
        client.DefaultRequestHeaders.Add("Api-Password", TestApiPassword);
        return client;
    }
}
