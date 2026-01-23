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

### Future Enhancements (vNext)
- 📋 Custom user-defined profiles
- 📋 3rd party API format compatibility (specific format TBD)
- 📋 REST API exposure

## Critical Information Needed

### 🚨 BLOCKING: 3rd Party API Specification

To ensure this is a true drop-in replacement, we need:

1. **API Provider Name**: Which service are we replacing?
2. **Documentation**: Links or examples of the API output
3. **Exact Field Specifications**:
   - Field names (e.g., "timestamp" vs "readingTime" vs "intervalEnd")
   - Data types and formats
   - Required vs optional fields
   - Metadata fields (if any)
4. **Format Examples**:
   ```json
   // Example needed
   {
     "?": "?",
     "?": "?"
   }
   ```
   ```csv
   // Example needed
   ?,?,?
   ```
5. **Special Requirements**:
   - Specific date/time formatting
   - Units and precision
   - Headers or metadata
   - File naming conventions

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
| Meter IDs | GUIDs | Uniqueness guarantee |
| Output Formats | CSV + JSON | Universal compatibility |
| CLI Framework | System.CommandLine | Official Microsoft tool |
| Profile Customization | Hard-coded (v1) | Simplicity, vNext enhancement |
| Multi-Meter Limit | 1000 per run | Balance performance/utility |

## Success Criteria

### Phase 1
- Generate intervals for all core business types
- Support both 15 and 30-minute periods
- Deterministic and non-deterministic modes working
- Multi-meter generation (up to 1000)
- CSV and JSON output

### Phase 2 (Post API Specification)
- Match 3rd party API format exactly
- Pass compatibility tests
- Validation with real-world use cases

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
