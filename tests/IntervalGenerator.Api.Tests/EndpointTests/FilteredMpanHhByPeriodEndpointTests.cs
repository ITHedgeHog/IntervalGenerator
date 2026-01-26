using System.Net;
using FluentAssertions;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Api.Tests.TestHarness;
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
