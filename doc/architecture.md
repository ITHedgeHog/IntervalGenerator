# Architecture and Implementation Plan

## System Architecture

### High-Level Design

```
┌─────────────────────────────────────────────────────────────┐
│                   Interval Generator CLI/API                 │
├─────────────────────────────────────────────────────────────┤
│                    Configuration Layer                       │
│  - Interval Period (15min/30min)                            │
│  - Business Type                                             │
│  - Date Range                                                │
│  - Deterministic/Non-Deterministic Mode                     │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│              Generation Engine (Core)                        │
│  ┌──────────────────────────────────────────────┐           │
│  │    Random Number Generator Strategy          │           │
│  │  - Seeded (Deterministic)                   │           │
│  │  - Non-Seeded (Non-Deterministic)           │           │
│  └──────────────────────────────────────────────┘           │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│           Business Profile Registry                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Office     │  │ Manufacturing│  │    Retail    │      │
│  │   Profile    │  │   Profile    │  │   Profile    │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
│  - Base Load Characteristics                                │
│  - Time-of-Day Patterns                                     │
│  - Day-of-Week Variations                                   │
│  - Seasonal Adjustments                                     │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│              Data Output Layer                               │
│  - CSV Export                                                │
│  - JSON Export                                               │
│  - Database Insert (Optional)                               │
│  - Stream/API Response                                       │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Interval Generator Core (`IntervalGenerator.Core`)

**Responsibilities:**
- Orchestrate the generation process
- Manage time period calculations
- Coordinate between profiles and output
- Handle multi-meter generation (up to 1000 meters)

**Key Classes:**
```csharp
- IntervalGeneratorEngine
- MultiMeterOrchestrator // Manages parallel generation for multiple meters
- IntervalPeriod (enum: FifteenMinute, ThirtyMinute)
- GenerationConfiguration
- MeterConfiguration
- GenerationResult
```

**Multi-Meter Support:**
- Generate unique GUIDs for each meter
- Support parallel processing for better performance
- Stream output to handle large datasets efficiently
- Maintain deterministic behavior across all meters when seeded

### 2. Random Number Strategy (`IntervalGenerator.Core.Randomization`)

**Responsibilities:**
- Provide deterministic and non-deterministic random number generation
- Ensure reproducibility when needed

**Key Classes:**
```csharp
- IRandomNumberGenerator (interface)
- DeterministicRandomGenerator (uses seeded Random)
- NonDeterministicRandomGenerator (uses Random.Shared or time-based seed)
- RandomGeneratorFactory
```

### 3. Business Profiles (`IntervalGenerator.Profiles`)

**Responsibilities:**
- Define consumption characteristics for different business types
- Calculate consumption values based on time, day, and season

**Key Classes:**
```csharp
- IConsumptionProfile (interface)
- OfficeProfile
- ManufacturingProfile
- RetailProfile
- DataCenterProfile
- EducationalProfile
- ProfileRegistry
```

**Profile Components:**
```csharp
- BaseLoadCalculator: Determines minimum consumption
- TimeOfDayModifier: Adjusts consumption based on hour
- DayOfWeekModifier: Adjusts for weekdays vs weekends
- SeasonalModifier: Adjusts for seasonal variations
- VariationGenerator: Adds realistic noise/variation
```

### 4. Output Formatters (`IntervalGenerator.Output`)

**Responsibilities:**
- Format and export generated data in Electralink API formats
- Support streaming for large multi-meter datasets
- Implement exact Electralink response schemas (HHPerPeriod, EacAdditionalDetailsV2)

**Key Classes:**
```csharp
- IOutputFormatter (interface)
- ElectraLinkJsonFormatter
  - HHPerPeriodFormatter (nested MC/date/period structure)
  - EacAdditionalDetailsV2Formatter
  - YearlyHHByPeriodFormatter
- ElectraLinkCsvFormatter
  - Flattened CSV structure
- StreamingOutputWriter // For efficient large-dataset output
```

**Output Formats:**
- **JSON**: Nested structure matching Electralink HHPerPeriod schema
- **CSV**: Flattened format with MPAN, MeasurementClass, Date, Period, HHC columns

**Reference:** See `doc/electralink-api-specification.md` for detailed response structures.

### 5. CLI Application (`IntervalGenerator.Cli`)

**Responsibilities:**
- Command-line interface for user interaction
- Configuration parsing
- Execution coordination

**Technology:**
- System.CommandLine for CLI parsing

### 6. API Application (Optional - `IntervalGenerator.Api`)

**Responsibilities:**
- REST API for generating intervals on-demand
- Web-based configuration

**Technology:**
- ASP.NET Core Minimal API

## Implementation Phases

### Phase 1: Foundation (Week 1)
- [ ] Set up .NET solution structure
- [ ] Create core domain models (with GUID meter IDs)
- [ ] Implement interval period calculations
- [ ] Create basic configuration system (supporting 1-1000 meters)
- [ ] Implement randomization strategy pattern (deterministic/non-deterministic)
- [ ] Create MultiMeterOrchestrator for parallel generation

### Phase 2: Basic Profile Implementation (Week 1-2)
- [ ] Design IConsumptionProfile interface
- [ ] Implement OfficeProfile with basic patterns
- [ ] Implement ManufacturingProfile
- [ ] Create ProfileRegistry
- [ ] Add unit tests for profiles

### Phase 3: Generation Engine (Week 2)
- [ ] Implement IntervalGeneratorEngine
- [ ] Integrate randomization strategies
- [ ] Add time-based modifiers (time-of-day, day-of-week)
- [ ] Implement seasonal adjustments
- [ ] Add variation/noise generation

### Phase 4: Output Layer (Week 2-3)
- [ ] Implement CSV output formatter
- [ ] Implement JSON output formatter
- [ ] Add console output for debugging
- [ ] Create output formatter factory

### Phase 5: CLI Application (Week 3)
- [ ] Set up System.CommandLine
- [ ] Implement generate command
- [ ] Add configuration options
- [ ] Implement help and documentation
- [ ] Add validation

### Phase 6: Testing & Documentation (Week 3-4)
- [ ] Unit tests for all components
- [ ] Integration tests
- [ ] Performance benchmarks
- [ ] User documentation
- [ ] Code samples

### Phase 7: Advanced Features (Week 4+)
- [ ] Additional business profiles
- [ ] Custom profile definition (JSON/YAML)
- [ ] API implementation
- [ ] Database output option
- [ ] Batch generation capabilities

## Technical Specifications

### .NET Version
- Target: .NET 8.0 (LTS)
- Language: C# 12

### Project Structure
```
IntervalGenerator/
├── src/
│   ├── IntervalGenerator.Core/
│   │   ├── Models/
│   │   ├── Services/
│   │   └── Randomization/
│   ├── IntervalGenerator.Profiles/
│   │   ├── Interfaces/
│   │   ├── Office/
│   │   ├── Manufacturing/
│   │   └── Retail/
│   ├── IntervalGenerator.Output/
│   │   ├── Formatters/
│   │   └── Writers/
│   └── IntervalGenerator.Cli/
│       ├── Commands/
│       └── Program.cs
├── tests/
│   ├── IntervalGenerator.Core.Tests/
│   ├── IntervalGenerator.Profiles.Tests/
│   └── IntervalGenerator.Integration.Tests/
├── doc/
│   ├── architecture.md
│   ├── assumptions-and-questions.md
│   └── user-guide.md
├── agents.md
└── README.md
```

### Data Models

#### IntervalReading
```csharp
public record IntervalReading
{
    public DateTime Timestamp { get; init; }
    public decimal ConsumptionKwh { get; init; }
    public Guid MeterId { get; init; }
    public string BusinessType { get; init; }
}
```

#### GenerationConfiguration
```csharp
public record GenerationConfiguration
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public IntervalPeriod Period { get; init; }
    public string BusinessType { get; init; }
    public bool Deterministic { get; init; }
    public int? Seed { get; init; }
    public int MeterCount { get; init; } = 1; // Support 1-1000 meters per run
    public List<Guid>? MeterIds { get; init; } // Optional: specify exact meter IDs
}
```

#### MeterConfiguration
```csharp
public record MeterConfiguration
{
    public Guid MeterId { get; init; }
    public string BusinessType { get; init; }
    public decimal BaseLoadMultiplier { get; init; } = 1.0m; // Optional variation per meter
}
```

### Algorithm Overview

**For each interval:**
1. Calculate base load for business type
2. Apply time-of-day modifier (0.0 - 2.0 multiplier)
3. Apply day-of-week modifier (0.5 - 1.0 multiplier for weekends)
4. Apply seasonal modifier (0.8 - 1.2 multiplier)
5. Add realistic variation/noise (±5-15%)
6. Ensure non-negative values

**Example Calculation:**
```
FinalConsumption = BaseLoad
                   × TimeOfDayModifier
                   × DayOfWeekModifier
                   × SeasonalModifier
                   × (1 + RandomVariation)
```

### Performance Targets

**Single Meter:**
- Generate 1 year of 15-minute intervals (35,040 readings) in < 1 second
- Generate 10 years of data in < 5 seconds
- Memory usage: < 100MB for 1 year of data

**Multi-Meter (up to 1000 meters):**
- Generate 1 year for 100 meters (3.5M readings) in < 10 seconds
- Generate 1 year for 1000 meters (35M readings) in < 60 seconds
- Memory usage: Streaming output to avoid loading all data in memory
- Support parallel generation for multiple meters

### Quality Attributes

1. **Testability**: All components interface-based with dependency injection
2. **Extensibility**: Easy to add new business profiles
3. **Configurability**: Flexible configuration options
4. **Performance**: Efficient generation for large time periods
5. **Repeatability**: Deterministic mode provides identical output with same seed

## Technology Stack

- **Framework**: .NET 8.0
- **Language**: C# 12
- **CLI**: System.CommandLine
- **Testing**: xUnit, FluentAssertions, NSubstitute
- **CSV**: CsvHelper
- **JSON**: System.Text.Json
- **DI Container**: Microsoft.Extensions.DependencyInjection
- **Configuration**: Microsoft.Extensions.Configuration
- **Logging**: Microsoft.Extensions.Logging

## Deployment

- **Package**: NuGet package for library components
- **CLI Tool**: .NET Global Tool (`dotnet tool install`)
- **API**: Docker container (optional)
