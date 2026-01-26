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
        data.Site.Should().NotBeNull();
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

        // Assert - each day should have 48 periods for 30-min intervals
        data.Should().NotBeNull();
        foreach (var mc in data!.MC.Values)
        {
            foreach (var dateData in mc.Values)
            {
                dateData.Count.Should().Be(48);
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

        // Assert
        var firstMc = data!.MC.First().Value;
        var firstDate = firstMc.First().Value;
        var firstPeriod = firstDate["1"];

        firstPeriod.Period.Should().Be(1);
        firstPeriod.Hhc.Should().BeGreaterThan(0);
        firstPeriod.Aei.Should().Be("A"); // Deterministic mode = all Actual
        firstPeriod.QtyId.Should().Be("kWh");
    }
}
