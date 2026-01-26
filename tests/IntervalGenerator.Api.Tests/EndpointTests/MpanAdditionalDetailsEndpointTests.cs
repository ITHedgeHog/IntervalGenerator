using System.Net;
using FluentAssertions;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Api.Tests.TestHarness;
using Xunit;

namespace IntervalGenerator.Api.Tests.EndpointTests;

public class MpanAdditionalDetailsEndpointTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;
    private readonly HttpClient _client;

    public MpanAdditionalDetailsEndpointTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetAdditionalDetails_WithValidMpan_ReturnsOk()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={testMpan}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAdditionalDetails_WithValidMpan_ReturnsCorrectStructure()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={testMpan}");
        var data = await response.Content.ReadFromJsonAsync<EacAdditionalDetailsResponse>();

        // Assert
        data.Should().NotBeNull();
        data!.Mpan.Should().Be(testMpan);
        data.Capacity.Should().NotBeNullOrEmpty();
        data.EnergisationStatus.Should().Be("Energised");
        data.SupplierId.Should().NotBeNullOrEmpty();
        data.PostCode.Should().NotBeNullOrEmpty();
        data.AdditionalDetail.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAdditionalDetails_WithInvalidMpan_ReturnsNotFound()
    {
        // Arrange
        var invalidMpan = "0000000000000";

        // Act
        var response = await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={invalidMpan}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAdditionalDetails_WithMissingMpan_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/v2/mpanadditionaldetails?mpan=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAdditionalDetails_HasCorrectAddressFields()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={testMpan}");
        var data = await response.Content.ReadFromJsonAsync<EacAdditionalDetailsResponse>();

        // Assert
        data.Should().NotBeNull();
        data!.MeteringPointAddressLine1.Should().NotBeNullOrEmpty();
        data.MeteringPointAddressLine3.Should().NotBeNullOrEmpty(); // City
        data.PostCode.Should().MatchRegex(@"^[A-Z]{1,2}\d{1,2}[A-Z]?\s?\d[A-Z]{2}$"); // UK postcode format
    }

    [Fact]
    public async Task GetAdditionalDetails_HasCorrectAdditionalDetailStructure()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={testMpan}");
        var data = await response.Content.ReadFromJsonAsync<EacAdditionalDetailsResponse>();

        // Assert
        data.Should().NotBeNull();
        data!.AdditionalDetail.Should().HaveCount(1);

        var detail = data.AdditionalDetail.First();
        detail.MeterId.Should().StartWith("M");
        detail.MeasurementClassId.Should().Be("AI");
        detail.AssetProviderId.Should().StartWith("PROVIDER");
    }

    [Fact]
    public async Task GetAdditionalDetails_WithCsvResponseType_ReturnsCsv()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        using var client = _fixture.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Add("response-type", "csv");

        // Act
        var response = await client.GetAsync($"/v2/mpanadditionaldetails?mpan={testMpan}");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
        content.Should().Contain("mpan,capacity,energisation_status");
    }
}
