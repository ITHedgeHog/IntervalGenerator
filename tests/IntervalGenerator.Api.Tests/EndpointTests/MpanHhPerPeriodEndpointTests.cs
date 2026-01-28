using System.Net;
using System.Text.Json;
using FluentAssertions;
using IntervalGenerator.Api.Endpoints;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Api.Tests.TestHarness;
using Xunit;

namespace IntervalGenerator.Api.Tests.EndpointTests;

public class MpanHhPerPeriodEndpointTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    public MpanHhPerPeriodEndpointTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetHhPerPeriod_WithValidMpan_ReturnsOk()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHhPerPeriod_WithValidMpan_ReturnsCorrectStructure()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");
        var data = await response.Content.ReadFromJsonAsync<HhPerPeriodResponse>();

        // Assert
        data.Should().NotBeNull();
        data!.MPAN.Should().Be(testMpan);
        data.MC.Should().NotBeNull();
        data.MC.Should().ContainKey("AI"); // Should have Active Import
    }

    [Fact]
    public async Task GetHhPerPeriod_WithInvalidMpan_ReturnsNotFound()
    {
        // Arrange
        var invalidMpan = "0000000000000";

        // Act
        var response = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={invalidMpan}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetHhPerPeriod_WithMissingMpan_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/v2/mpanhhperperiod?mpan=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetHhPerPeriod_WithCsvResponseType_ReturnsCsv()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        using var client = _fixture.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Add("response-type", "csv");

        // Act
        var response = await client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        content.Should().StartWith("MPAN,Site,MeasurementClass,Date,Period,HHC,AEI,QtyId");
    }

    [Fact]
    public async Task GetHhPerPeriod_HasCorrectPeriodCount()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");
        var data = await response.Content.ReadFromJsonAsync<HhPerPeriodResponse>();

        // Assert - each day should have 50 periods (P1-P48 + P49/P50 with null values)
        data.Should().NotBeNull();
        foreach (var mc in data!.MC.Values)
        {
            foreach (var dateData in mc.Values)
            {
                dateData.Count.Should().Be(50);
            }
        }
    }

    [Fact]
    public async Task GetHhPerPeriod_PeriodDataHasCorrectFields()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");
        var data = await response.Content.ReadFromJsonAsync<HhPerPeriodResponse>();

        // Assert - period keys are P-prefixed (P1, P2, etc.)
        var firstMc = data!.MC.First().Value;
        var firstDate = firstMc.First().Value;
        var firstPeriod = firstDate["P1"];

        // HHC is a string value, AEI is "A" for Actual
        firstPeriod.HHC.Should().NotBeNull();
        decimal.Parse(firstPeriod.HHC!, System.Globalization.CultureInfo.InvariantCulture).Should().BeGreaterThan(0);
        firstPeriod.AEI.Should().Be("A"); // Deterministic mode = all Actual

        // P49 and P50 should have null values
        var p49 = firstDate["P49"];
        p49.HHC.Should().BeNull();
        p49.AEI.Should().BeNull();
    }
}
