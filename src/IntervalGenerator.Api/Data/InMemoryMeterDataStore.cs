using System.Collections.Concurrent;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Core.Models;
using IntervalGenerator.Core.Profiles;
using IntervalGenerator.Core.Randomization;
using IntervalGenerator.Core.Services;
using IntervalGenerator.Core.Utilities;
using IntervalGenerator.Profiles;
using Microsoft.Extensions.Logging;

namespace IntervalGenerator.Api.Data;

/// <summary>
/// In-memory implementation of the meter data store.
/// Pre-generates all meter data at startup for fast API responses.
/// </summary>
public sealed class InMemoryMeterDataStore : IMeterDataStore
{
    private readonly ILogger<InMemoryMeterDataStore> _logger;
    private readonly ConcurrentDictionary<string, List<IntervalReading>> _readings = new();
    private readonly ConcurrentDictionary<string, MeterDetails> _meterDetails = new();
    private bool _isInitialized;

    // Business types to cycle through for varied profiles
    private static readonly string[] BusinessTypes = ["Office", "Manufacturing", "Retail", "DataCenter", "Educational"];

    // Sample addresses for realistic data
    private static readonly string[] Streets = ["High Street", "Main Street", "Station Road", "Church Lane", "Park Avenue", "Victoria Road", "Mill Lane", "School Road", "Market Square", "Bridge Street"];
    private static readonly string[] Cities = ["London", "Manchester", "Birmingham", "Leeds", "Bristol", "Liverpool", "Sheffield", "Newcastle", "Edinburgh", "Cardiff"];
    private static readonly string[] PostCodes = ["SW1A 1AA", "M1 1AE", "B1 1AA", "LS1 1BA", "BS1 1AA", "L1 1JD", "S1 1AA", "NE1 1AA", "EH1 1AA", "CF10 1AA"];

    public InMemoryMeterDataStore(ILogger<InMemoryMeterDataStore> logger)
    {
        _logger = logger;
    }

    public bool IsInitialized => _isInitialized;
    public int MeterCount => _meterDetails.Count;

    public async Task InitializeAsync(int meterCount, GenerationConfiguration baseConfig)
    {
        if (_isInitialized)
        {
            _logger.LogWarning("Store already initialized with {MeterCount} meters", MeterCount);
            return;
        }

        _logger.LogInformation("Initializing meter data store with {MeterCount} meters...", meterCount);
        var startTime = DateTime.UtcNow;

        var registry = new ProfileRegistry();
        var tasks = new List<Task>();

        for (int i = 0; i < meterCount; i++)
        {
            var meterIndex = i;
            tasks.Add(Task.Run(() => GenerateMeterData(meterIndex, meterCount, baseConfig, registry)));
        }

        await Task.WhenAll(tasks);

        _isInitialized = true;
        var elapsed = DateTime.UtcNow - startTime;
        _logger.LogInformation(
            "Initialized {MeterCount} meters with {TotalReadings:N0} total readings in {ElapsedMs:N0}ms",
            MeterCount,
            _readings.Values.Sum(r => r.Count),
            elapsed.TotalMilliseconds);
    }

    private void GenerateMeterData(int meterIndex, int totalMeters, GenerationConfiguration baseConfig, ProfileRegistry registry)
    {
        // Generate deterministic meter ID based on index
        var meterId = GenerateDeterministicGuid(baseConfig.Seed ?? 42, meterIndex);
        var mpan = MpanGenerator.GenerateMpan(meterId);

        // Cycle through business types for variety
        var businessType = BusinessTypes[meterIndex % BusinessTypes.Length];
        var profile = registry.GetProfile(businessType);

        // Create meter-specific configuration
        var config = baseConfig with
        {
            BusinessType = businessType,
            MeterCount = 1,
            MeterIds = [meterId]
        };

        // Create random generator
        var random = RandomGeneratorFactory.Create(config);

        // Generate readings
        var engine = new IntervalGeneratorEngine(profile, random);
        var readings = engine.GenerateReadings(
            meterId,
            mpan,
            config.StartDate,
            config.EndDate,
            config.Period,
            config.MeasurementClass).ToList();

        // Store readings
        _readings[mpan] = readings;

        // Generate meter details
        var addressIndex = meterIndex % Streets.Length;
        var details = new MeterDetails
        {
            Mpan = mpan,
            MeterId = meterId,
            SiteName = $"{businessType} Site {meterIndex + 1}",
            BusinessType = businessType,
            Capacity = ((meterIndex % 5 + 1) * 100).ToString(),
            Address = new MeterAddress
            {
                Line1 = $"{(meterIndex + 1) * 10} {Streets[addressIndex]}",
                Line2 = $"Unit {meterIndex + 1}",
                Line3 = Cities[addressIndex],
                PostCode = PostCodes[addressIndex]
            },
            SupplierId = $"SUPPLIER{(meterIndex % 10 + 1):D3}",
            AssetProviderId = $"PROVIDER{(meterIndex % 5 + 1):D3}"
        };

        _meterDetails[mpan] = details;
    }

    private static Guid GenerateDeterministicGuid(int seed, int index)
    {
        // Generate deterministic GUID from seed and index
        var random = new Random(seed + index);
        var bytes = new byte[16];
        random.NextBytes(bytes);
        return new Guid(bytes);
    }

    public IEnumerable<IntervalReading> GetReadings(
        string mpan,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        MeasurementClass? measurementClass = null)
    {
        if (!_readings.TryGetValue(mpan, out var readings))
        {
            return Enumerable.Empty<IntervalReading>();
        }

        IEnumerable<IntervalReading> result = readings;

        if (startDate.HasValue)
        {
            var start = startDate.Value.ToDateTime(TimeOnly.MinValue);
            result = result.Where(r => r.Timestamp >= start);
        }

        if (endDate.HasValue)
        {
            var end = endDate.Value.ToDateTime(TimeOnly.MaxValue);
            result = result.Where(r => r.Timestamp <= end);
        }

        if (measurementClass.HasValue)
        {
            result = result.Where(r => r.MeasurementClass == measurementClass.Value);
        }

        return result;
    }

    public MeterDetails? GetMeterDetails(string mpan)
    {
        return _meterDetails.TryGetValue(mpan, out var details) ? details : null;
    }

    public IEnumerable<string> GetAllMpans()
    {
        return _meterDetails.Keys;
    }

    public bool MpanExists(string mpan)
    {
        return _meterDetails.ContainsKey(mpan);
    }
}
