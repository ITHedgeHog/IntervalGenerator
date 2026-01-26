using IntervalGenerator.Api.Authentication;
using IntervalGenerator.Api.Data;
using IntervalGenerator.Api.Endpoints;
using IntervalGenerator.Api.Models;
using IntervalGenerator.Core.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

// Add data store as singleton (shared across requests)
builder.Services.AddSingleton<IMeterDataStore, InMemoryMeterDataStore>();

var app = builder.Build();

// Initialize meter data store
await InitializeMeterDataStore(app);

// Use API key authentication
app.UseApiKeyAuthentication();

// Map endpoints
app.MapMpanHhPerPeriodEndpoint();
app.MapMpanAdditionalDetailsEndpoint();
app.MapFilteredMpanHhByPeriodEndpoint();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .WithTags("Health");

// Root endpoint with API info
app.MapGet("/", (IMeterDataStore store) => Results.Ok(new
{
    name = "Electralink EAC API (Mock)",
    version = "1.0.0",
    metersLoaded = store.MeterCount,
    endpoints = new[]
    {
        "/v2/mpanhhperperiod?mpan={mpan}",
        "/v2/mpanadditionaldetails?mpan={mpan}",
        "/v1/filteredmpanhhbyperiod?mpan={mpan}&StartDate={date}&EndDate={date}"
    }
}))
    .WithName("ApiInfo")
    .WithTags("Info")
    .ExcludeFromDescription();

// List all MPANs endpoint (for testing)
app.MapGet("/mpans", (IMeterDataStore store) => Results.Ok(new
{
    count = store.MeterCount,
    mpans = store.GetAllMpans().ToList()
}))
    .WithName("ListMpans")
    .WithTags("Info")
    .WithDescription("List all available MPANs in the mock API");

app.Run();

async Task InitializeMeterDataStore(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var store = app.Services.GetRequiredService<IMeterDataStore>();
    var settings = app.Configuration.GetSection("ApiSettings:MeterGeneration").Get<MeterGenerationSettings>() ?? new();

    logger.LogInformation("Initializing meter data store with settings: {@Settings}", settings);

    var config = new GenerationConfiguration
    {
        StartDate = DateOnly.Parse(settings.DefaultStartDate).ToDateTime(TimeOnly.MinValue),
        EndDate = DateOnly.Parse(settings.DefaultEndDate).ToDateTime(TimeOnly.MaxValue),
        Period = settings.DefaultIntervalPeriod switch
        {
            5 => IntervalPeriod.FiveMinute,
            15 => IntervalPeriod.FifteenMinute,
            _ => IntervalPeriod.ThirtyMinute
        },
        BusinessType = settings.DefaultBusinessType,
        Deterministic = settings.DeterministicMode,
        Seed = settings.Seed,
        MeterCount = settings.DefaultMeterCount
    };

    await store.InitializeAsync(settings.DefaultMeterCount, config);

    logger.LogInformation("Meter data store initialized with {MeterCount} meters", store.MeterCount);
}

// Make Program class accessible for testing
public partial class Program { }
