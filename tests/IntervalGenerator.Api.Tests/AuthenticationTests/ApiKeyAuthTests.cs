using System.Net;
using FluentAssertions;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Api.Tests.TestHarness;
using Xunit;

namespace IntervalGenerator.Api.Tests.AuthenticationTests;

public class ApiKeyAuthTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public ApiKeyAuthTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Request_WithValidCredentials_Succeeds()
    {
        // Arrange
        using var client = _fixture.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/mpans");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_WithoutCredentials_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _fixture.CreateUnauthenticatedClient();
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithInvalidApiKey_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "wrong-key");
        client.DefaultRequestHeaders.Add("X-Api-Password", ApiTestFixture.TestApiPassword);

        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiTestFixture.TestApiKey);
        client.DefaultRequestHeaders.Add("X-Api-Password", "wrong-password");

        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithMissingApiKey_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Password", ApiTestFixture.TestApiPassword);

        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_WithMissingPassword_ReturnsUnauthorized()
    {
        // Arrange
        using var client = _fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", ApiTestFixture.TestApiKey);

        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HealthCheck_WithoutCredentials_Succeeds()
    {
        // Arrange
        using var client = _fixture.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RootEndpoint_WithoutCredentials_Succeeds()
    {
        // Arrange
        using var client = _fixture.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnauthorizedResponse_HasCorrectFormat()
    {
        // Arrange
        using var client = _fixture.CreateUnauthenticatedClient();
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();

        // Assert
        error.Should().NotBeNull();
        error!.Error.Should().Be("Unauthorized");
        error.Status.Should().Be(401);
    }
}
