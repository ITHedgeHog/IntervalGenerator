# Requirements Summary

## Project Goal
Create a .NET-based smart meter interval generator that produces realistic energy consumption data and serves as a drop-in replacement for a 3rd party API.

## Confirmed Requirements

### Core Functionality
- ✅ Generate 15-minute or 30-minute interval data
- ✅ Support multiple business types: Office, Manufacturing, Retail, etc.
- ✅ Deterministic mode (seeded random) for testing
- ✅ Non-deterministic mode for realistic simulation

### Multi-Meter Support
- ✅ Generate up to 1000 meters per run
- ✅ Each meter identified by unique GUID
- ✅ All meters in single output file/stream

### Output
- ✅ CSV format
- ✅ JSON format
- ✅ No database integration in v1

#### Drop-In API Replacement
- ✅ **Electralink EAC API** - Confirmed target
  - `/v2/mpanhhperperiod` endpoint (HHPerPeriod format)
  - `/v2/mpanadditionaldetails` endpoint (EacAdditionalDetailsV2 format)
  - `/v1/filteredmpanhhbyperiod` endpoint (YearlyHHByPeriodOutput format)
- ✅ JSON output format with nested MC/date/period structure
- ✅ CSV output with flattened structure
- ✅ MPAN-based meter identification (13-digit numeric ID)
- ✅ Period-based interval representation (1-48 for 30min, 1-96 for 15min)

### Future Enhancements (vNext)
- 📋 Custom user-defined profiles
- 📋 REST API exposure for on-demand generation
- 📋 XML output format
- 📋 Additional measurement classes (AE, RI, RE)

## Open Questions (Non-Blocking)

### Lower Priority
1. **Validation**: Should we provide statistics/validation output?
2. **Precision**: Is 2 decimal places sufficient for kWh values?
3. **Time Zones**: UTC only, or configurable time zones?
4. **Holidays**: Should profiles account for public holidays?
5. **Historical Accuracy**: Match weather patterns or generic seasonal?
6. **Anomalies**: Include optional data anomalies for testing?

## Development Approach

### Can Start Now
- Core architecture and domain models
- Randomization strategies
- Business profile implementations
- Interval calculation logic
- Multi-meter orchestration

### Needs API Specification
- Output formatter implementation
- Data model field names
- Validation logic
- Integration tests with real format

## Technical Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Framework | .NET 8.0 | LTS support, modern features |
| Language | C# 12 | Latest language features |
| Target API | Electralink EAC | Energy Account Centre for smart meters |
| API Endpoints | /v2/mpanhhperperiod, /v2/mpanadditionaldetails | Primary data endpoints |
| Response Formats | HHPerPeriod, EacAdditionalDetailsV2 | Electralink schemas |
| Meter IDs | GUIDs (internal) → MPAN (output) | Uniqueness + Electralink compatibility |
| Output Formats | CSV + JSON | Electralink compatibility |
| CLI Framework | System.CommandLine | Official Microsoft tool |
| Profile Customization | Hard-coded (v1) | Simplicity, vNext enhancement |
| Multi-Meter Limit | 1000 per run | Balance performance/utility |

## Success Criteria

### Phase 1: Core Generation Engine
- ✅ Generate intervals for all core business types
- ✅ Support both 15 and 30-minute periods
- ✅ Deterministic and non-deterministic modes working
- ✅ Multi-meter generation (up to 1000)
- ✅ Consume Electralink OpenAPI spec

### Phase 2: Electralink API Compatibility
- Output matches HHPerPeriod JSON schema exactly
- Output matches EacAdditionalDetailsV2 schema
- CSV format with correct column mapping
- MPAN derivation from GUID
- Period numbering (1-48 or 1-96)
- Consumption values (hhc) in kWh with 2 decimals
- Status flags (aei) implementation
- Multi-meter output with unique MPANs

### Phase 3: Validation & Integration
- Parse generated output with Electralink API schema validators
- Integration tests with Electralink staging endpoints
- Real-world use case validation

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Unknown API format | High - blocks output design | Proceed with core engine, defer output layer details |
| Performance at 1000 meters | Medium | Implement streaming, benchmark early |
| Profile accuracy | Low | Start simple, refine based on feedback |
| Time zone complexity | Low | Start with UTC, add TZ support if needed |

## Immediate Action Items

1. 🔴 **CRITICAL**: Obtain 3rd party API specification
2. 🟢 **START**: Phase 1 core development
3. 🟡 **PLAN**: Prepare output layer for API format integration
