# Electralink API Implementation Plan

## Executive Summary

This document outlines the implementation plan for a drop-in replacement Electralink EAC (Energy Account Centre) API. The API will serve generated interval data through REST endpoints that match the exact Electralink API response formats.

## Current State

The project already has:
- **Core Generation Engine**: `IntervalGeneratorEngine` and `MultiMeterOrchestrator` for generating interval data
- **Output Formatters**: `ElectralinkJsonFormatter` and `CsvOutputFormatter` producing Electralink-compatible formats
- **Business Profiles**: 5 consumption profiles (Office, Manufacturing, Retail, DataCenter, Educational)
- **CLI Application**: Command-line interface for batch generation

## What Needs to Be Implemented

### 1. API Server (`IntervalGenerator.Api`)

A new ASP.NET Core Minimal API project providing REST endpoints that match Electralink's API.

### 2. Data Store

An in-memory meter store to:
- Pre-generate meters with their interval data
- Support MPAN-based lookups
- Handle date range queries
- Support multiple measurement classes

### 3. API Endpoints

| Endpoint | Method | Description | Priority |
|----------|--------|-------------|----------|
| `/v2/mpanhhperperiod` | GET | Half-hourly per-period data (primary) | **P0** |
| `/v2/mpanadditionaldetails` | GET | Meter metadata and address | **P1** |
| `/v1/filteredmpanhhbyperiod` | GET | Filtered historical data (legacy) | **P2** |

### 4. Authentication

Simple API key/password header authentication matching Electralink's scheme.

---

## Detailed Implementation Plan

### Phase 1: API Project Setup

**Goal**: Create the API project structure and basic infrastructure.

**Tasks**:
1. Create `IntervalGenerator.Api` project with ASP.NET Core Minimal API
2. Add project references to Core, Profiles, and Output
3. Configure services and dependency injection
4. Add Swagger/OpenAPI documentation
5. Set up configuration for API settings

**Files to Create**:
```
src/IntervalGenerator.Api/
├── IntervalGenerator.Api.csproj
├── Program.cs
├── appsettings.json
└── appsettings.Development.json
```

---

### Phase 2: Meter Data Store

**Goal**: Implement an in-memory store for pre-generated meter data.

**Design**:
```csharp
public interface IMeterDataStore
{
    // Initialization
    Task InitializeAsync(int meterCount, GenerationConfiguration config);

    // Query operations
    IEnumerable<IntervalReading> GetReadings(string mpan, DateOnly? startDate = null, DateOnly? endDate = null);
    MeterDetails? GetMeterDetails(string mpan);
    IEnumerable<string> GetAllMpans();
    bool MpanExists(string mpan);
}

public record MeterDetails
{
    public string Mpan { get; init; }
    public Guid MeterId { get; init; }
    public string SiteName { get; init; }
    public string BusinessType { get; init; }
    public string Capacity { get; init; }
    public string EnergisationStatus { get; init; }
    public DateTime EnergisationEffectiveDate { get; init; }
    public MeterAddress Address { get; init; }
    public string SupplierId { get; init; }
    public string LineLossFactorClassId { get; init; }
    public string SettlementConfigId { get; init; }
}

public record MeterAddress
{
    public string Line1 { get; init; }
    public string Line2 { get; init; }
    public string Line3 { get; init; }
    public string PostCode { get; init; }
}
```

**Files to Create**:
```
src/IntervalGenerator.Api/
├── Data/
│   ├── IMeterDataStore.cs
│   ├── InMemoryMeterDataStore.cs
│   ├── MeterDetails.cs
│   └── MeterAddress.cs
```

---

### Phase 3: API Endpoints Implementation

#### 3.1 `/v2/mpanhhperperiod` Endpoint (P0)

**Request**:
```
GET /v2/mpanhhperperiod?mpan=1266448934017
Headers:
  - response-type: json|csv (optional, default: json)
  - X-Api-Key: <key>
  - X-Api-Password: <password>
```

**Response** (JSON):
```json
{
  "MPAN": "1266448934017",
  "site": "Site Name",
  "MC": {
    "AI": {
      "2024-01-01": {
        "1": { "period": 1, "hhc": 2.5, "aei": "A", "qty_id": "kWh" },
        ...
        "48": { "period": 48, "hhc": 2.1, "aei": "A", "qty_id": "kWh" }
      }
    }
  }
}
```

**Implementation**:
```csharp
app.MapGet("/v2/mpanhhperperiod", async (
    [FromQuery] string mpan,
    [FromHeader(Name = "response-type")] string? responseType,
    IMeterDataStore store,
    IOutputFormatter jsonFormatter,
    IOutputFormatter csvFormatter) =>
{
    if (!store.MpanExists(mpan))
        return Results.NotFound(new { error = "MPAN not found" });

    var readings = store.GetReadings(mpan);
    var details = store.GetMeterDetails(mpan);

    if (responseType?.ToLower() == "csv")
    {
        // Return CSV
    }

    // Return JSON (default)
});
```

#### 3.2 `/v2/mpanadditionaldetails` Endpoint (P1)

**Request**:
```
GET /v2/mpanadditionaldetails?mpan=1266448934017
Headers:
  - response-type: json|csv|xml (optional, default: json)
```

**Response**:
```json
{
  "mpan": "1266448934017",
  "capacity": "100",
  "energisation_status": "Energised",
  "energisation_status_effective_from_date": "2020-01-01",
  "metering_point_address_line_1": "123 Main Street",
  "metering_point_address_line_2": "Building A",
  "metering_point_address_line_3": "London",
  "metering_point_address_line_4": "",
  "metering_point_address_line_5": "",
  "metering_point_address_line_6": "",
  "metering_point_address_line_7": "",
  "metering_point_address_line_8": "",
  "metering_point_address_line_9": "",
  "post_code": "SW1A 1AA",
  "supplier_id": "SUPPLIER001",
  "line_loss_factor_class_id": "EA",
  "line_loss_factor_class_id_effective_from_date": "2020-01-01",
  "standard_settlement_configuration_id": "401",
  "standard_settlement_configuration_id_effective_from_date": "2020-01-01",
  "disconnected_mpan": "false",
  "disconnection_date": null,
  "additional_detail": [
    {
      "meter_id": "M123456789",
      "measurement_class_id": "AI",
      "asset_provider_id": "PROVIDER001"
    }
  ]
}
```

#### 3.3 `/v1/filteredmpanhhbyperiod` Endpoint (P2)

**Request**:
```
GET /v1/filteredmpanhhbyperiod?mpan=1266448934017&StartDate=2024-01-01&EndDate=2024-01-31&MeasurementClass=AI
```

**Response**: `YearlyHHByPeriodOutput` format with filtered data.

---

### Phase 4: Authentication Middleware

**Implementation**:
```csharp
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiKeySettings _settings;

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health check endpoints
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
        var apiPassword = context.Request.Headers["X-Api-Password"].FirstOrDefault();

        if (!ValidateCredentials(apiKey, apiPassword))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        await _next(context);
    }
}
```

**Configuration**:
```json
{
  "ApiKey": {
    "Key": "test-api-key",
    "Password": "test-api-password",
    "Enabled": true
  }
}
```

---

### Phase 5: Response Format Models

Create DTOs that exactly match Electralink's response schemas.

**Files to Create**:
```
src/IntervalGenerator.Api/
├── Models/
│   ├── HhPerPeriodResponse.cs
│   ├── EacAdditionalDetailsResponse.cs
│   ├── YearlyHhByPeriodResponse.cs
│   └── ErrorResponse.cs
```

---

### Phase 6: Test Harness

**Goal**: Create a comprehensive test harness that:
1. Starts the API with 100 pre-generated meters
2. Validates all endpoints work correctly
3. Tests edge cases and error handling
4. Measures performance

**Test Harness Structure**:
```
tests/IntervalGenerator.Api.Tests/
├── IntervalGenerator.Api.Tests.csproj
├── TestHarness/
│   ├── ApiTestFixture.cs        # Shared test server setup
│   ├── TestDataGenerator.cs     # 100 meter data generator
│   └── ResponseValidator.cs     # Validates response formats
├── EndpointTests/
│   ├── MpanHhPerPeriodTests.cs
│   ├── MpanAdditionalDetailsTests.cs
│   └── FilteredMpanHhByPeriodTests.cs
├── AuthenticationTests/
│   └── ApiKeyAuthTests.cs
└── PerformanceTests/
    └── LoadTests.cs
```

**Test Scenarios**:
1. **Happy Path**: Valid MPAN returns correct data format
2. **Not Found**: Invalid MPAN returns 404
3. **Date Filtering**: Date range queries work correctly
4. **Response Format**: JSON and CSV content negotiation
5. **Authentication**: Valid/invalid API keys
6. **Performance**: Response time under load
7. **Multi-Meter**: Test all 100 meters sequentially and in parallel

---

## Project Structure (Final)

```
IntervalGenerator/
├── src/
│   ├── IntervalGenerator.Core/          # (existing)
│   ├── IntervalGenerator.Profiles/      # (existing)
│   ├── IntervalGenerator.Output/        # (existing)
│   ├── IntervalGenerator.Cli/           # (existing)
│   └── IntervalGenerator.Api/           # NEW
│       ├── IntervalGenerator.Api.csproj
│       ├── Program.cs
│       ├── appsettings.json
│       ├── Authentication/
│       │   └── ApiKeyAuthenticationMiddleware.cs
│       ├── Data/
│       │   ├── IMeterDataStore.cs
│       │   ├── InMemoryMeterDataStore.cs
│       │   └── MeterDetailsGenerator.cs
│       ├── Endpoints/
│       │   ├── MpanHhPerPeriodEndpoint.cs
│       │   ├── MpanAdditionalDetailsEndpoint.cs
│       │   └── FilteredMpanHhByPeriodEndpoint.cs
│       └── Models/
│           ├── HhPerPeriodResponse.cs
│           ├── EacAdditionalDetailsResponse.cs
│           ├── YearlyHhByPeriodResponse.cs
│           └── ApiSettings.cs
├── tests/
│   ├── IntervalGenerator.Core.Tests/    # (existing)
│   ├── IntervalGenerator.Profiles.Tests/# (existing)
│   ├── IntervalGenerator.Integration.Tests/ # (existing)
│   └── IntervalGenerator.Api.Tests/     # NEW
│       ├── IntervalGenerator.Api.Tests.csproj
│       ├── TestHarness/
│       │   ├── ApiTestFixture.cs
│       │   └── HundredMeterTestHarness.cs
│       ├── EndpointTests/
│       └── PerformanceTests/
└── doc/
    └── electralink-api-implementation-plan.md # THIS FILE
```

---

## Dependencies to Add

### IntervalGenerator.Api.csproj
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
  <PackageReference Include="Swashbuckle.AspNetCore" Version="7.0.0" />
</ItemGroup>

<ItemGroup>
  <ProjectReference Include="..\IntervalGenerator.Core\IntervalGenerator.Core.csproj" />
  <ProjectReference Include="..\IntervalGenerator.Profiles\IntervalGenerator.Profiles.csproj" />
  <ProjectReference Include="..\IntervalGenerator.Output\IntervalGenerator.Output.csproj" />
</ItemGroup>
```

### IntervalGenerator.Api.Tests.csproj
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="FluentAssertions" Version="8.0.0" />
</ItemGroup>
```

---

## API Configuration

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ApiSettings": {
    "Authentication": {
      "Enabled": true,
      "ApiKey": "demo-api-key",
      "ApiPassword": "demo-api-password"
    },
    "MeterGeneration": {
      "DefaultMeterCount": 100,
      "DefaultStartDate": "2024-01-01",
      "DefaultEndDate": "2024-12-31",
      "DefaultIntervalPeriod": 30,
      "DefaultBusinessType": "Office",
      "DeterministicMode": true,
      "Seed": 42
    }
  }
}
```

---

## Test Harness: 100 Meter Test

### Test Data Generation
```csharp
public class HundredMeterTestHarness
{
    private readonly HttpClient _client;
    private readonly List<string> _mpans;

    public async Task GenerateAndValidate100Meters()
    {
        // 1. Initialize API with 100 meters (mix of profiles)
        // 2. Retrieve all MPANs
        // 3. For each MPAN:
        //    - Call /v2/mpanhhperperiod
        //    - Validate response structure
        //    - Verify data quality flags
        //    - Check period coverage (1-48)
        // 4. Test date filtering
        // 5. Test concurrent requests
        // 6. Generate performance report
    }
}
```

### Expected Test Coverage
- **100 meters** with varied profiles (20 Office, 20 Manufacturing, 20 Retail, 20 DataCenter, 20 Educational)
- **1 year of data** per meter (17,520 readings each at 30-min intervals)
- **Total readings**: 1,752,000 readings
- **Endpoints tested**: All 3 primary endpoints
- **Response formats**: JSON and CSV

---

## Success Criteria

1. **API Compatibility**: All endpoints return data matching Electralink's exact response format
2. **Performance**: API responds in <100ms for single meter queries, <1s for 100 meter batch
3. **Test Coverage**: 100% endpoint coverage, >80% code coverage
4. **Documentation**: Swagger UI accessible at `/swagger`
5. **Test Harness**: Successfully queries all 100 meters without errors

---

## Implementation Timeline

| Phase | Description | Dependencies |
|-------|-------------|--------------|
| 1 | API Project Setup | None |
| 2 | Meter Data Store | Phase 1 |
| 3 | Endpoint Implementation | Phases 1, 2 |
| 4 | Authentication | Phase 1 |
| 5 | Response Models | Phase 3 |
| 6 | Test Harness | Phases 1-5 |

---

## Next Steps

1. Create the API project structure
2. Implement the meter data store
3. Implement `/v2/mpanhhperperiod` endpoint (P0)
4. Create the 100-meter test harness
5. Implement remaining endpoints
6. Add comprehensive test coverage
