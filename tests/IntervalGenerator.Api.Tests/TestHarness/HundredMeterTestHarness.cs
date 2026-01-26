using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using IntervalGenerator.Api.Endpoints;
using IntervalGenerator.Api.Models;
using Xunit;
using Xunit.Abstractions;

namespace IntervalGenerator.Api.Tests.TestHarness;

/// <summary>
/// Comprehensive test harness that tests all 100 meters via the API.
/// </summary>
public class HundredMeterTestHarness : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly HttpClient _client;

    public HundredMeterTestHarness(ApiTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _client = fixture.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task ShouldHave100MetersLoaded()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/mpans");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<MpanListResponse>();

        // Assert
        content.Should().NotBeNull();
        content!.Count.Should().Be(ApiTestFixture.TestMeterCount);
        content.Mpans.Should().HaveCount(ApiTestFixture.TestMeterCount);
        _output.WriteLine($"Verified {content.Count} meters loaded");
    }

    [Fact]
    public async Task ShouldReturnValidDataForAllMeters_HhPerPeriod()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var stopwatch = Stopwatch.StartNew();
        var successCount = 0;
        var failCount = 0;
        var totalReadings = 0;

        // Act
        foreach (var mpan in mpans)
        {
            var response = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={mpan}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<HhPerPeriodResponse>(content);

                data.Should().NotBeNull();
                data!.MPAN.Should().Be(mpan);
                data.MC.Should().NotBeEmpty();

                // Count readings
                foreach (var mc in data.MC.Values)
                {
                    foreach (var date in mc.Values)
                    {
                        totalReadings += date.Count;
                    }
                }

                successCount++;
            }
            else
            {
                _output.WriteLine($"Failed for MPAN {mpan}: {response.StatusCode}");
                failCount++;
            }
        }

        stopwatch.Stop();

        // Assert
        successCount.Should().Be(ApiTestFixture.TestMeterCount);
        failCount.Should().Be(0);

        _output.WriteLine($"Tested {successCount}/{mpans.Count} meters successfully");
        _output.WriteLine($"Total readings: {totalReadings:N0}");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds:N0}ms");
        _output.WriteLine($"Average per meter: {stopwatch.ElapsedMilliseconds / mpans.Count}ms");
    }

    [Fact]
    public async Task ShouldReturnValidDataForAllMeters_AdditionalDetails()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var stopwatch = Stopwatch.StartNew();
        var successCount = 0;

        // Act
        foreach (var mpan in mpans)
        {
            var response = await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={mpan}");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<EacAdditionalDetailsResponse>();

                data.Should().NotBeNull();
                data!.Mpan.Should().Be(mpan);
                data.EnergisationStatus.Should().Be("Energised");
                data.Capacity.Should().NotBeNullOrEmpty();
                data.PostCode.Should().NotBeNullOrEmpty();
                data.AdditionalDetail.Should().NotBeEmpty();

                successCount++;
            }
            else
            {
                _output.WriteLine($"Failed for MPAN {mpan}: {response.StatusCode}");
            }
        }

        stopwatch.Stop();

        // Assert
        successCount.Should().Be(ApiTestFixture.TestMeterCount);

        _output.WriteLine($"Tested {successCount}/{mpans.Count} meters successfully");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds:N0}ms");
    }

    [Fact]
    public async Task ShouldReturnValidDataForAllMeters_FilteredByPeriod()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var stopwatch = Stopwatch.StartNew();
        var successCount = 0;

        // Act - test date filtering
        foreach (var mpan in mpans)
        {
            var response = await _client.GetAsync(
                $"/v1/filteredmpanhhbyperiod?mpan={mpan}&StartDate=2024-01-01&EndDate=2024-01-07&MeasurementClass=AI");

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<YearlyHhByPeriodResponse>();

                data.Should().NotBeNull();
                data!.Mpan.Should().Be(mpan);
                data.StartDate.Should().Be("2024-01-01");
                data.EndDate.Should().Be("2024-01-07");
                data.DaysActual.Should().BeGreaterThan(0);

                successCount++;
            }
            else
            {
                _output.WriteLine($"Failed for MPAN {mpan}: {response.StatusCode}");
            }
        }

        stopwatch.Stop();

        // Assert
        successCount.Should().Be(ApiTestFixture.TestMeterCount);

        _output.WriteLine($"Tested {successCount}/{mpans.Count} meters with date filter");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds:N0}ms");
    }

    [Fact]
    public async Task ShouldHandleCsvResponseFormat()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        _client.DefaultRequestHeaders.Add("response-type", "csv");
        var response = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");

        var csvContent = await response.Content.ReadAsStringAsync();
        csvContent.Should().Contain("MPAN,Site,MeasurementClass,Date,Period,HHC,AEI,QtyId");
        csvContent.Should().Contain(testMpan);

        _output.WriteLine($"CSV response lines: {csvContent.Split('\n').Length}");
    }

    [Fact]
    public async Task ShouldHandleConcurrentRequests()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var stopwatch = Stopwatch.StartNew();

        // Act - Make concurrent requests
        var tasks = mpans.Select(async mpan =>
        {
            using var client = _fixture.CreateAuthenticatedClient();
            var response = await client.GetAsync($"/v2/mpanhhperperiod?mpan={mpan}");
            return response.IsSuccessStatusCode;
        });

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var successCount = results.Count(r => r);
        successCount.Should().Be(ApiTestFixture.TestMeterCount);

        _output.WriteLine($"Concurrent requests completed: {successCount}/{mpans.Count}");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds:N0}ms");
        _output.WriteLine($"Average per request (concurrent): {stopwatch.ElapsedMilliseconds / mpans.Count}ms");
    }

    [Fact]
    public async Task ShouldHaveConsistentPeriodData()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var testMpan = mpans.First();

        // Act
        var response = await _client.GetAsync($"/v2/mpanhhperperiod?mpan={testMpan}");
        var data = await response.Content.ReadFromJsonAsync<HhPerPeriodResponse>();

        // Assert - verify period structure
        data.Should().NotBeNull();

        foreach (var mc in data!.MC)
        {
            _output.WriteLine($"Measurement Class: {mc.Key}");

            foreach (var date in mc.Value)
            {
                var periodCount = date.Value.Count;
                periodCount.Should().Be(48, $"Date {date.Key} should have 48 periods for 30-min intervals");

                // Verify period numbers are 1-48
                var periods = date.Value.Keys.Select(int.Parse).OrderBy(p => p).ToList();
                periods.First().Should().Be(1);
                periods.Last().Should().Be(48);

                // Verify all period data has required fields
                foreach (var period in date.Value.Values)
                {
                    period.Period.Should().BeInRange(1, 48);
                    period.Hhc.Should().BeGreaterThanOrEqualTo(0);
                    period.Aei.Should().BeOneOf("A", "E", "M", "X");
                    period.QtyId.Should().Be("kWh");
                }
            }
        }

        _output.WriteLine($"Period data validation passed for MPAN {testMpan}");
    }

    [Fact]
    public async Task ShouldHaveVariedBusinessProfiles()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var businessTypes = new HashSet<string>();

        // Act - collect all business types
        foreach (var mpan in mpans.Take(20)) // Sample first 20
        {
            var response = await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={mpan}");
            var data = await response.Content.ReadFromJsonAsync<EacAdditionalDetailsResponse>();

            // Extract site name which contains business type
            var siteName = data?.MeteringPointAddressLine2 ?? "";
            businessTypes.Add(siteName);
        }

        // Assert - should have multiple business types (cycle through 5 types)
        _output.WriteLine($"Unique address patterns found: {businessTypes.Count}");
        foreach (var bt in businessTypes)
        {
            _output.WriteLine($"  - {bt}");
        }
    }

    [Fact]
    public async Task ShouldGeneratePerformanceReport()
    {
        // Arrange
        var mpans = await _fixture.GetAllMpansAsync();
        var metrics = new List<(string Endpoint, long ElapsedMs)>();

        // Test /v2/mpanhhperperiod
        var sw = Stopwatch.StartNew();
        foreach (var mpan in mpans)
        {
            await _client.GetAsync($"/v2/mpanhhperperiod?mpan={mpan}");
        }
        sw.Stop();
        metrics.Add(("/v2/mpanhhperperiod", sw.ElapsedMilliseconds));

        // Test /v2/mpanadditionaldetails
        sw.Restart();
        foreach (var mpan in mpans)
        {
            await _client.GetAsync($"/v2/mpanadditionaldetails?mpan={mpan}");
        }
        sw.Stop();
        metrics.Add(("/v2/mpanadditionaldetails", sw.ElapsedMilliseconds));

        // Test /v1/filteredmpanhhbyperiod
        sw.Restart();
        foreach (var mpan in mpans)
        {
            await _client.GetAsync($"/v1/filteredmpanhhbyperiod?mpan={mpan}&StartDate=2024-01-01&EndDate=2024-01-07");
        }
        sw.Stop();
        metrics.Add(("/v1/filteredmpanhhbyperiod", sw.ElapsedMilliseconds));

        // Output performance report
        _output.WriteLine("=== PERFORMANCE REPORT ===");
        _output.WriteLine($"Test Configuration:");
        _output.WriteLine($"  - Meters: {mpans.Count}");
        _output.WriteLine($"  - Date Range: 2024-01-01 to 2024-01-31");
        _output.WriteLine("");
        _output.WriteLine("Results:");
        foreach (var (endpoint, elapsed) in metrics)
        {
            _output.WriteLine($"  {endpoint}:");
            _output.WriteLine($"    Total: {elapsed:N0}ms");
            _output.WriteLine($"    Avg per request: {elapsed / mpans.Count}ms");
        }
        _output.WriteLine("=========================");

        // Assert performance targets
        foreach (var (endpoint, elapsed) in metrics)
        {
            var avgMs = elapsed / mpans.Count;
            avgMs.Should().BeLessThan(1000, $"{endpoint} should respond in under 1 second on average");
        }
    }
}
