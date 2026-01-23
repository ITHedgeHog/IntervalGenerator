# Assumptions and Questions

## ✅ 3rd Party API Format - RESOLVED

**API Provider**: Electralink EAC (Energy Account Centre)

**Target Endpoints**:
- `/v2/mpanhhperperiod` - Half-hourly consumption per measurement class
- `/v2/mpanadditionaldetails` - Meter metadata and address details
- `/v1/filteredmpanhhbyperiod` - Filtered historical data

**Key Specifications**:
- Response formats: JSON and CSV
- MPAN format: 13-digit meter identifier
- Period range: 1-48 (30-minute intervals) or 1-96 (15-minute intervals)
- Consumption unit: kWh with 2 decimal places
- Status flags: A (Actual), E (Estimated), M (Missing), X (Corrected)
- Measurement classes: AI (Active Import), AE (Active Export), RI/RE (Reactive)

**Detailed Specification**: See `doc/electralink-api-specification.md`

---

## Assumptions

### Technical Assumptions

1. **Target Framework**
   - Assumption: Using .NET 8.0 (current LTS version)
   - Rationale: Provides long-term support and modern C# features
   - Question: Is there a requirement for .NET Framework compatibility or specific .NET version?

2. **Interval Alignment**
   - Assumption: Intervals align to standard boundaries (e.g., 00:00, 00:15, 00:30, 00:45 for 15-min intervals)
   - Rationale: This matches real smart meter behavior
   - Question: Are there scenarios where intervals need custom alignment?

3. **Time Zones**
   - Assumption: All timestamps will be in UTC or a configurable single time zone
   - Rationale: Simplifies initial implementation
   - Question: Do we need to support multiple time zones or DST transitions?

4. **Data Volume**
   - Assumption: Typical generation runs will be 1-10 years of data
   - Rationale: Sufficient for testing and modeling purposes
   - Question: What are the maximum expected data volumes?

5. **Meter ID Format**
   - Assumption: Meter IDs are GUIDs (Globally Unique Identifiers)
   - Rationale: Ensures uniqueness across all generated meters
   - **DECIDED**: Each meter will have a unique GUID in the output to identify itself

### Business Logic Assumptions

6. **Consumption Units**
   - Assumption: All consumption values are in kWh (kilowatt-hours)
   - Rationale: Standard unit for energy consumption
   - Question: Do we need to support other units (kW, MWh, etc.)?

7. **Office Profile Characteristics**
   - Assumptions:
     - Operating hours: 8 AM - 6 PM on weekdays
     - Base load: 50-200 kWh per interval (adjustable)
     - Weekend consumption: 20% of weekday
     - Seasonal variation: ±20% (higher in summer for AC, winter for heating)
   - Question: Are these characteristics aligned with target use cases?

8. **Manufacturing Profile Characteristics**
   - Assumptions:
     - 24/7 operation OR shift-based (3 shifts)
     - High base load: 200-800 kWh per interval
     - Minimal day-of-week variation
     - Seasonal variation: ±10%
   - Question: Should we support both continuous and shift-based manufacturing?

9. **Retail Profile Characteristics**
   - Assumptions:
     - Operating hours: 9 AM - 9 PM daily
     - Weekend peak periods (higher consumption)
     - Moderate base load: 30-150 kWh per interval
     - Seasonal peaks during holidays
   - Question: Do we need to model specific retail sub-types (grocery, mall, boutique)?

10. **Variation/Noise**
    - Assumption: Random variation should be ±5-15% to simulate real-world fluctuations
    - Rationale: Real meters show natural variation
    - Question: Should variation be configurable per profile?

11. **Seasonal Patterns**
    - Assumption: Northern hemisphere seasons (summer = Jun-Aug, winter = Dec-Feb)
    - Rationale: Standard for initial implementation
    - Question: Do we need hemisphere configuration or custom seasonal definitions?

12. **Negative Values**
    - Assumption: Consumption values cannot be negative (no generation/export modeling)
    - Rationale: Smart meters typically track consumption, not generation
    - Question: Do we need to model net metering or solar export scenarios?

### Output Assumptions

13. **Output Formats**
    - Assumption: CSV and JSON are primary output formats
    - **DECIDED**: CSV and JSON only for initial release
    - Placeholder CSV Format (subject to 3rd party API requirements):
      ```
      Timestamp,MeterId,ConsumptionKwh,BusinessType
      2024-01-01T00:00:00Z,550e8400-e29b-41d4-a716-446655440000,125.5,Office
      ```
    - **PENDING**: Exact format will be determined by 3rd party API specification

14. **Precision**
    - Assumption: Consumption values rounded to 2 decimal places
    - Rationale: Matches typical meter precision
    - Question: Is higher precision needed for specific use cases?

15. **File Size**
    - Assumption: Large datasets will be streamed to avoid memory issues
    - Rationale: Years of 15-minute data can be substantial
    - Question: Are there specific file size limits or chunking requirements?

## Open Questions

### CRITICAL - Requires Immediate Clarification

1. **3rd Party API Format** ⚠️ BLOCKING
   - Which 3rd party API format should we match?
   - What is the exact data structure, field names, and conventions?
   - Do we need to expose a REST API, or just match their output format?
   - Where can we find documentation/examples of their format?
   - **Impact**: This affects core data models, output formatters, and potentially architecture

### Requirements Clarification

2. **Validation Requirements**
   - Should the generator validate that output matches expected totals?
   - Should it provide statistics (min, max, average, total consumption)?

3. **Historical Accuracy**
   - Do generated patterns need to match historical weather data?
   - Or are generic seasonal patterns acceptable?

### ✅ ANSWERED Questions

4. **Profile Customization** ✅
   - **DECIDED**: Custom profiles are vNext (next version) - hard-coded profiles for initial release

5. **API Requirements** ✅
   - **DECIDED**: 3rd party API format to be adopted in subsequent phases
   - CLI tool is primary interface for initial release

6. **Database Integration** ✅
   - **DECIDED**: CSV and JSON output only for initial release
   - No database integration needed initially

7. **Multi-Meter Support** ✅
   - **DECIDED**: Yes, support generating up to 1000 meters per run
   - Each meter identified by unique GUID

6. **Historical Accuracy**
   - Do generated patterns need to match historical weather data?
   - Or are generic seasonal patterns acceptable?

### Deterministic vs Non-Deterministic

7. **Deterministic Mode**
   - What is the primary use case for deterministic mode?
     - Automated testing?
     - Reproducible research?
     - Regression testing?
   - Should seed be required or auto-generated based on configuration hash?

8. **Non-Deterministic Mode**
   - Should each run produce completely different results?
   - Or should there be some consistency with controlled randomness?

### Performance and Scale

9. **Concurrency**
   - Should the generator support parallel processing for multiple meters?
   - Is single-threaded generation acceptable?

10. **Memory Constraints**
    - Are there specific memory usage limitations?
    - Should we optimize for low-memory environments?

11. **Real-time Generation**
    - Is there a need for real-time/streaming generation?
    - Or is batch generation sufficient?

### Business Logic Details

12. **Holiday Handling**
    - Should the generator account for public holidays?
    - If so, which holiday calendars (US, UK, EU, etc.)?

13. **Special Events**
    - Should profiles support one-off events (shutdowns, maintenance, special operations)?
    - Or just recurring patterns?

14. **Ramping Periods**
    - Should consumption ramp up/down gradually (e.g., office building warming up in morning)?
    - Or use step functions?

15. **Anomaly Generation**
    - Should the generator optionally include anomalies (spikes, dropouts) for testing?
    - What types of anomalies are relevant?

### Data Quality

16. **Missing Data**
    - Should the generator optionally simulate missing intervals?
    - This could be useful for testing gap-handling logic

17. **Data Validation**
    - Should output include quality flags or confidence scores?
    - Or just raw consumption values?

### Configuration

18. **Configuration Format**
    - Command-line arguments only?
    - Configuration files (JSON, YAML, TOML)?
    - Both?

19. **Presets**
    - Should we provide preset configurations for common scenarios?
    - Examples: "typical-office", "24x7-manufacturing", etc.

20. **Extensibility**
    - Should the system support plugins or custom profile assemblies?
    - Or is source code modification acceptable for customization?

## Decision Log

| Date | Question | Decision | Rationale |
|------|----------|----------|-----------|
| 2026-01-23 | .NET Version | .NET 8.0 | LTS support, modern features |
| 2026-01-23 | Primary Output | CSV + JSON | Common formats, easy to parse |
| 2026-01-23 | CLI Framework | System.CommandLine | Official Microsoft library |
| 2026-01-23 | Meter ID Format | GUID (Globally Unique ID) | Ensures uniqueness, industry standard |
| 2026-01-23 | Multi-Meter Support | Yes, up to 1000 meters/run | Enables bulk generation scenarios |
| 2026-01-23 | Custom Profiles | Deferred to vNext | Focus on core profiles first |
| 2026-01-23 | Database Output | Not in initial release | CSV/JSON sufficient for v1 |
| 2026-01-23 | 3rd Party API | Match format in later phase | Drop-in replacement capability |

## Next Steps

1. **IMMEDIATE: Get 3rd Party API Details** ⚠️
   - Identify the specific 3rd party API to replicate
   - Obtain documentation, examples, and field specifications
   - Document exact output format requirements
   - Update data models and output formatters accordingly

2. **Phase 1 Development (Can Proceed in Parallel)**
   - Set up .NET solution structure
   - Implement core domain models (using placeholder output format)
   - Create randomization strategies
   - Build basic business profiles
   - Note: Output layer will need revision once API format is confirmed

3. **Validation and Refinement**
   - Validate business profile characteristics
   - Test with small datasets
   - Verify multi-meter generation (up to 1000 meters)
   - Iterate based on feedback

## Notes

- This is a living document and should be updated as the project evolves
- Assumptions should be validated before major implementation efforts
- Open questions should be resolved during planning or early implementation phases
- Some questions may be deferred to later phases based on priority
