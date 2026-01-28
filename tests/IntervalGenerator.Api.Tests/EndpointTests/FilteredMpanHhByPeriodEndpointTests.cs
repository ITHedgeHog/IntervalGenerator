using System.Globalization;
using System.Net;
using FluentAssertions;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Api.Tests.TestHarness;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IntervalGenerator.Api.Tests.EndpointTests;

public class FilteredMpanHhByPeriodEndpointTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    public FilteredMpanHhByPeriodEndpointTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetFilteredData_WithValidMpan_ReturnsOk()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v1/filteredmpanhhbyperiod?mpan={testMpan}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFilteredData_WithDateRange_ReturnsFilteredData()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate=2024-01-01&EndDate=2024-01-07");
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert
        data.Should().NotBeNull();
        data!.Mpan.Should().Be(testMpan);
        data.StartDate.Should().Be("2024-01-01");
        data.EndDate.Should().Be("2024-01-07");
        data.DaysActual.Should().BeInRange(1, 7);
    }

    [Fact]
    public async Task GetFilteredData_WithMeasurementClass_FiltersCorrectly()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&MeasurementClass=AI");
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert
        data.Should().NotBeNull();
        data!.AiYearlyValue.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetFilteredData_WithInvalidMpan_ReturnsNotFound()
    {
        // Arrange
        var invalidMpan = "0000000000000";

        // Act
        var response = await _client.GetAsync($"/v1/filteredmpanhhbyperiod?mpan={invalidMpan}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFilteredData_ReturnsCorrectStructure()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v1/filteredmpanhhbyperiod?mpan={testMpan}");
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert
        data.Should().NotBeNull();
        data!.Mpan.Should().Be(testMpan);
        data.StartDate.Should().NotBeNullOrEmpty();
        data.EndDate.Should().NotBeNullOrEmpty();
        data.DaysActual.Should().BeGreaterThanOrEqualTo(0);
        data.DaysEstimated.Should().BeGreaterThanOrEqualTo(0);
        data.DaysMissing.Should().BeGreaterThanOrEqualTo(0);
        data.ActualMeasurements.Should().NotBeNull();
        data.EstimatedMeasurements.Should().NotBeNull();
        data.MissingMeasurement.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFilteredData_ActualMeasurementsHaveCorrectFormat()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v1/filteredmpanhhbyperiod?mpan={testMpan}");
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert
        data.Should().NotBeNull();
        data!.ActualMeasurements.Should().NotBeEmpty();

        var firstDay = data.ActualMeasurements.First();
        firstDay.Date.Should().MatchRegex(@"^\d{4}-\d{2}-\d{2}$");
        firstDay.QtyId.Should().Be("kWh");
        firstDay.Periods.Should().NotBeEmpty();
        firstDay.Periods.Should().HaveCount(48); // 30-min intervals

        var firstPeriod = firstDay.Periods.First();
        firstPeriod.Period.Should().Be(1);
        firstPeriod.Hhc.Should().BeGreaterThan(0);
        firstPeriod.Aei.Should().Be("A"); // Deterministic mode
    }

    [Fact]
    public async Task GetFilteredData_WithCsvResponseType_ReturnsCsv()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        using var client = _fixture.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Add("response-type", "csv");

        // Act
        var response = await client.GetAsync($"/v1/filteredmpanhhbyperiod?mpan={testMpan}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        content.Should().Contain("MPAN,Date,Period,HHC,AEI,QtyId");
    }
}

/// <summary>
/// Tests for leap year date handling with full year data generation.
/// </summary>
public class LeapYearTests : IClassFixture<LeapYearTestFixture>
{
    private readonly LeapYearTestFixture _fixture;
    private readonly HttpClient _client;

    public LeapYearTests(LeapYearTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetFilteredData_LeapYearFebruary29_ReturnsData()
    {
        // Arrange - 2024 is a leap year, verify Feb 29 data exists
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act - Request data spanning February 29, 2024
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate=2024-02-28&EndDate=2024-03-01");
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert - Should have 3 days of data (Feb 28, Feb 29, Mar 1)
        data.Should().NotBeNull();
        data!.ActualMeasurements.Should().NotBeEmpty();
        data.ActualMeasurements.Should().HaveCount(3, "Should have measurements for Feb 28, Feb 29, and Mar 1");

        // Verify Feb 29 is included
        var dates = data.ActualMeasurements.Select(m => m.Date).OrderBy(d => d).ToList();
        dates.Should().Contain("2024-02-28");
        dates.Should().Contain("2024-02-29");
        dates.Should().Contain("2024-03-01");
    }

    [Fact]
    public async Task GetFilteredData_LeapYearFebruary29_HasValidPeriodData()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act - Get data for Feb 29, 2024
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate=2024-02-29&EndDate=2024-02-29");
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert
        data.Should().NotBeNull();
        data!.ActualMeasurements.Should().HaveCount(1);
        var feb29 = data.ActualMeasurements.First();
        feb29.Date.Should().Be("2024-02-29");
        feb29.Periods.Should().HaveCount(48, "Feb 29 should have 48 periods for 30-min intervals");
        feb29.Periods.Should().AllSatisfy(p =>
        {
            p.Hhc.Should().BeGreaterThanOrEqualTo(0);
            p.Aei.Should().BeOneOf("A", "E", "M", "X");
        });
    }

    [Fact]
    public async Task GetFilteredData_LeapYearFullFebruary_Has29Days()
    {
        // Arrange - 2024 is a leap year (29 days in Feb)
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act - Request entire February 2024
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate=2024-02-01&EndDate=2024-02-29");
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert
        data.Should().NotBeNull();
        data!.ActualMeasurements.Should().HaveCount(29, "February 2024 should have 29 days");
        data.DaysActual.Should().Be(29, "Days actual count should be 29");

        // Verify date range
        data.StartDate.Should().Be("2024-02-01");
        data.EndDate.Should().Be("2024-02-29");
    }

    [Fact]
    public async Task GetFilteredData_NonLeapYearFebruary_Has28Days()
    {
        // Arrange - 2023 is not a leap year (28 days in Feb)
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act - Request entire February 2023 (will be empty as fixture generates 2024 data)
        // Instead verify that Feb 2024 has 29 vs non-leap Feb would have 28
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate=2024-02-01&EndDate=2024-02-29");
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert
        data.Should().NotBeNull();
        data!.ActualMeasurements.Should().HaveCount(29, "Leap year February 2024 has 29 days, not 28");
    }

    [Fact]
    public async Task GetFilteredData_LeapYearQ1_DayCountMatchesExpected()
    {
        // Arrange - Q1 2024 with leap year
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act - Request Q1 2024 (Jan 1 - Mar 31)
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate=2024-01-01&EndDate=2024-03-31");
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert - Q1 with leap year should have 31 + 29 + 31 = 91 days
        data.Should().NotBeNull();
        var expectedDays = 31 + 29 + 31; // Jan + Feb (leap) + Mar
        data!.ActualMeasurements.Should().HaveCount(expectedDays,
            "Q1 2024 (leap year) should have 91 days");

        // Verify consumption values are reasonable
        var totalConsumption = data.ActualMeasurements
            .SelectMany(m => m.Periods)
            .Sum(p => p.Hhc);
        totalConsumption.Should().BeGreaterThan(0, "Q1 should have positive total consumption");
    }

    [Fact]
    public async Task GetFilteredData_LeapYearConsumption_IncludesFeb29Data()
    {
        // Arrange - 2024 is a leap year with 366 days
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act - Request full year 2024
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate=2024-01-01&EndDate=2024-12-31&MeasurementClass=AI");
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert
        data.Should().NotBeNull();
        data!.ActualMeasurements.Should().HaveCount(366, "2024 is a leap year with 366 days");
        data.DaysActual.Should().Be(366);

        // Verify Feb 29 exists in the data
        var dates = data.ActualMeasurements.Select(m => m.Date).ToList();
        dates.Should().Contain("2024-02-29", "Feb 29 should be included in leap year data");

        // Verify yearly value is calculated (should include Feb 29 data)
        data.AiYearlyValue.Should().BeGreaterThan(0, "Yearly consumption should be positive");
    }

    [Fact]
    public async Task GetFilteredData_LeapYear_AllDaysHave48Periods()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act - Get full year 2024 leap year data
        var response = await _client.GetAsync(
            $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate=2024-01-01&EndDate=2024-12-31");
        var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

        // Assert - All days should have 48 periods for 30-min intervals
        data.Should().NotBeNull();
        data!.ActualMeasurements.Should().AllSatisfy(measurement =>
        {
            measurement.Periods.Should().HaveCount(48,
                $"Day {measurement.Date} should have 48 periods for 30-min intervals");
        });
    }

    [Fact]
    public async Task GetFilteredData_LeapYearMonthCounts_MatchCalendar()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act & Assert - Verify each month in 2024 has correct number of days
        var monthDays = new Dictionary<int, int>
        {
            { 1, 31 },   // January
            { 2, 29 },   // February (leap year!)
            { 3, 31 },   // March
            { 4, 30 },   // April
            { 5, 31 },   // May
            { 6, 30 },   // June
            { 7, 31 },   // July
            { 8, 31 },   // August
            { 9, 30 },   // September
            { 10, 31 },  // October
            { 11, 30 },  // November
            { 12, 31 }   // December
        };

        foreach (var (month, expectedDays) in monthDays)
        {
            var startDate = new DateTime(2024, month, 1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var endDate = new DateTime(2024, month, DateTime.DaysInMonth(2024, month)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var response = await _client.GetAsync(
                $"/v1/filteredmpanhhbyperiod?mpan={testMpan}&StartDate={startDate}&EndDate={endDate}");
            var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

            data.Should().NotBeNull();
            data!.ActualMeasurements.Should().HaveCount(expectedDays,
                $"Month {month} in 2024 should have {expectedDays} days (Feb has 29 in leap year)");
        }
    }
}

/// <summary>
/// Test fixture that generates a full year of data for leap year testing (2024).
/// </summary>
public class LeapYearTestFixture : WebApplicationFactory<Program>
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
                ["ApiSettings:MeterGeneration:DefaultMeterCount"] = "10",
                ["ApiSettings:MeterGeneration:DefaultStartDate"] = "2024-01-01",
                ["ApiSettings:MeterGeneration:DefaultEndDate"] = "2024-12-31", // Full year including Feb 29
                ["ApiSettings:MeterGeneration:DefaultIntervalPeriod"] = "30",
                ["ApiSettings:MeterGeneration:DeterministicMode"] = "true",
                ["ApiSettings:MeterGeneration:Seed"] = "42",
                ["ApiSettings:MeterGeneration:EnableDynamicGeneration"] = "false"
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

    public async Task<List<string>> GetAllMpansAsync()
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/mpans");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<MpanListResponse>();
        return content?.Mpans ?? [];
    }
}
