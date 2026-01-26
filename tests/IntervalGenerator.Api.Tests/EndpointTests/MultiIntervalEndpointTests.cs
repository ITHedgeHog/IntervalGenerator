using FluentAssertions;
using IntervalGenerator.Api.Endpoints;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Api.Tests.TestHarness;
using Xunit;

namespace IntervalGenerator.Api.Tests.EndpointTests;

/// <summary>
/// Tests API endpoints with 5-minute interval data.
/// </summary>
public class FiveMinuteIntervalEndpointTests : IClassFixture<FiveMinuteApiTestFixture>
{
    private readonly FiveMinuteApiTestFixture _fixture;
    private readonly HttpClient _client;

    public FiveMinuteIntervalEndpointTests(FiveMinuteApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetMpanHhPerPeriod_FiveMinute_Returns288PeriodsPerDay()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");
        var data = await response.Content.ReadFromJsonAsync<HhPerPeriodResponse>();

        // Assert
        data.Should().NotBeNull();
        foreach (var mc in data!.MC.Values)
        {
            foreach (var dateData in mc.Values)
            {
                dateData.Count.Should().Be(288, "5-minute intervals should have 288 periods per day");
                var periodNumbers = dateData.Keys.Select(int.Parse).OrderBy(p => p).ToList();
                periodNumbers.Should().ContainInOrder(Enumerable.Range(1, 288));
            }
        }
    }

    [Fact]
    public async Task GetFilteredMpanHhByPeriod_FiveMinute_ReturnsData()
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
        data.ActualMeasurements.Should().NotBeEmpty();

        // Verify each day has 288 periods for 5-minute intervals
        foreach (var day in data.ActualMeasurements)
        {
            day.Periods.Count.Should().Be(288);
        }
    }

    [Fact]
    public async Task GetMpanAdditionalDetails_FiveMinute_ReturnsOk()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={testMpan}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<EacAdditionalDetailsResponse>();
        data.Should().NotBeNull();
        data!.Mpan.Should().Be(testMpan);
    }
}

/// <summary>
/// Tests API endpoints with 15-minute interval data.
/// </summary>
public class FifteenMinuteIntervalEndpointTests : IClassFixture<FifteenMinuteApiTestFixture>
{
    private readonly FifteenMinuteApiTestFixture _fixture;
    private readonly HttpClient _client;

    public FifteenMinuteIntervalEndpointTests(FifteenMinuteApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetMpanHhPerPeriod_FifteenMinute_Returns96PeriodsPerDay()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");
        var data = await response.Content.ReadFromJsonAsync<HhPerPeriodResponse>();

        // Assert
        data.Should().NotBeNull();
        foreach (var mc in data!.MC.Values)
        {
            foreach (var dateData in mc.Values)
            {
                dateData.Count.Should().Be(96, "15-minute intervals should have 96 periods per day");
                var periodNumbers = dateData.Keys.Select(int.Parse).OrderBy(p => p).ToList();
                periodNumbers.Should().ContainInOrder(Enumerable.Range(1, 96));
            }
        }
    }

    [Fact]
    public async Task GetFilteredMpanHhByPeriod_FifteenMinute_ReturnsData()
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
        data.ActualMeasurements.Should().NotBeEmpty();

        // Verify each day has 96 periods for 15-minute intervals
        foreach (var day in data.ActualMeasurements)
        {
            day.Periods.Count.Should().Be(96);
        }
    }

    [Fact]
    public async Task GetMpanAdditionalDetails_FifteenMinute_ReturnsOk()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={testMpan}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<EacAdditionalDetailsResponse>();
        data.Should().NotBeNull();
        data!.Mpan.Should().Be(testMpan);
    }
}

/// <summary>
/// Tests API endpoints with 30-minute interval data.
/// </summary>
public class ThirtyMinuteIntervalEndpointTests : IClassFixture<ThirtyMinuteApiTestFixture>
{
    private readonly ThirtyMinuteApiTestFixture _fixture;
    private readonly HttpClient _client;

    public ThirtyMinuteIntervalEndpointTests(ThirtyMinuteApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetMpanHhPerPeriod_ThirtyMinute_Returns48PeriodsPerDay()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");
        var data = await response.Content.ReadFromJsonAsync<HhPerPeriodResponse>();

        // Assert
        data.Should().NotBeNull();
        foreach (var mc in data!.MC.Values)
        {
            foreach (var dateData in mc.Values)
            {
                dateData.Count.Should().Be(48, "30-minute intervals should have 48 periods per day");
                var periodNumbers = dateData.Keys.Select(int.Parse).OrderBy(p => p).ToList();
                periodNumbers.Should().ContainInOrder(Enumerable.Range(1, 48));
            }
        }
    }

    [Fact]
    public async Task GetFilteredMpanHhByPeriod_ThirtyMinute_ReturnsData()
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
        data.ActualMeasurements.Should().NotBeEmpty();

        // Verify each day has 48 periods for 30-minute intervals
        foreach (var day in data.ActualMeasurements)
        {
            day.Periods.Count.Should().Be(48);
        }
    }

    [Fact]
    public async Task GetMpanAdditionalDetails_ThirtyMinute_ReturnsOk()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={testMpan}");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<EacAdditionalDetailsResponse>();
        data.Should().NotBeNull();
        data!.Mpan.Should().Be(testMpan);
    }
}
