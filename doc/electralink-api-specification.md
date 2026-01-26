# Electralink EAC API Specification

## Overview

The Interval Generator is designed to be a drop-in replacement for Electralink's EAC (Energy Account Centre) API. This document specifies how our generated data will map to Electralink's API response formats.

## Target API Endpoints

### 1. `/v2/mpanhhperperiod` - Half-Hourly Per-Period Data
**Purpose**: Retrieve half-hourly consumption data organized by measurement class and period

**Request Parameters:**
- `mpan` (integer, required): Unique meter identifier
- `response-type` (header, optional): Response format - "json" or "csv" (default: "json")

**Response Schema**: `domains.HHPerPeriod`

### 2. `/v2/mpanadditionaldetails` - Additional Meter Details
**Purpose**: Retrieve meter metadata and address information

**Request Parameters:**
- `mpan` (string, required): Unique meter identifier
- `response-type` (header, optional): Response format - "json", "xml", or "csv" (default: "json")

**Response Schema**: `domains.EacAdditionalDetailsV2`

### 3. `/v1/filteredmpanhhbyperiod` - Filtered Half-Hourly Data (v1)
**Purpose**: Legacy endpoint for filtered consumption data by date range

**Request Parameters:**
- `mpan` (string, required): Unique meter identifier
- `StartDate`, `EndDate` (string, Y-m-d format)
- `StartPeriod`, `EndPeriod` (string, period identifiers)
- `MeasurementClass` (string): Filter by class (AI, AIAE, AIAERI, etc.)
- `response-type` (header): json, xml, or csv

**Response Schema**: `domains.YearlyHHByPeriodOutput`

## Data Models

### 1. HHPerPeriod Response

This is the primary response structure for `/v2/mpanhhperperiod`.

```json
{
  "MPAN": "1266448934017",
  "site": "Site Name",
  "MC": {
    "AI": {
      "2024-01-01": {
        "1": { "period": 1, "hhc": 2.5, "aei": "A", "qty_id": "kWh" },
        "2": { "period": 2, "hhc": 2.3, "aei": "A", "qty_id": "kWh" },
        ...
        "48": { "period": 48, "hhc": 2.1, "aei": "A", "qty_id": "kWh" }
      },
      "2024-01-02": { ... }
    }
  }
}
```

**Key Fields:**
- `MPAN`: String representation of the meter ID
- `site`: Site/location name
- `MC`: Measurement Class dictionary
  - Keys: Measurement class codes (AI, AE, RI, RE, etc.)
  - Value: Date-keyed object
    - Date key (YYYY-MM-DD): Period number keyed object
      - Period number (1-48): Period data

**Period Data Structure:**
```typescript
{
  "period": number,        // 1-48 for 30-min, 1-96 for 15-min
  "hhc": number,           // Half-Hourly Consumption in kWh
  "aei": string,           // Status flag (A = Actual, E = Estimated, etc.)
  "qty_id": string         // Quantity identifier (e.g., "kWh")
}
```

### 2. EacAdditionalDetailsV2 Response

Response structure for `/v2/mpanadditionaldetails`.

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

### 3. YearlyHHByPeriodOutput Response (v2 version)

Alternative response structure for yearly data aggregation.

```json
{
  "mpan": "1266448934017",
  "start_date": "2023-01-01",
  "end_date": "2023-12-31",
  "days_actual": 365,
  "days_estimated": 0,
  "days_missing": 0,
  "ai_yearly_value": 15000.5,
  "ae_yearly_value": null,
  "ri_yearly_value": null,
  "re_yearly_value": null,
  "actual_measurements": [
    {
      "date": "2023-01-01",
      "qty_id": "kWh",
      "periods": [
        { "period": 1, "hhc": 2.5, "aei": "A" },
        { "period": 2, "hhc": 2.3, "aei": "A" },
        ...
        { "period": 48, "hhc": 2.1, "aei": "A" }
      ]
    }
  ],
  "estimated_measurements": [],
  "missing_measurement": []
}
```

## Data Generation Mapping

### MPAN to Generator IDs

**Current Approach:**
- Electralink expects MPAN as a numeric identifier (13 digits)
- Our generator uses GUIDs for meter identification
- **Mapping Strategy**:
  - Store GUID as internal meter ID
  - Convert GUID to numeric MPAN for API responses
  - Pattern: Use first 13 digits of GUID's hex representation as MPAN

**Example:**
```
GUID: 550e8400-e29b-41d4-a716-446655440000
MPAN: 1266448934017 (derived from GUID)
```

### Interval to Period Numbers

**30-Minute Intervals:**
- Period 1: 00:00-00:30
- Period 2: 00:30-01:00
- ...
- Period 48: 23:30-24:00

**15-Minute Intervals:**
- Period 1: 00:00-00:15
- Period 2: 00:15-00:30
- ...
- Period 96: 23:45-24:00

### Consumption Values

**Field Mapping:**
- Our generated consumption values (kWh) → `hhc` field
- Precision: 2 decimal places
- Unit: kWh (kilowatt-hours)

### Status Flags (aei)

**Possible Values:**
- `A`: Actual (measured data)
- `E`: Estimated
- `M`: Missing/Not recorded
- `X`: Corrected

**Generator Approach:**
- Deterministic mode: All `A` (actual)
- Non-Deterministic mode: Optionally mix in `E` (estimated) values for realism

### Measurement Classes

**Standard Classes (from Electralink):**
- `AI`: Active Import (consumption)
- `AE`: Active Export (generation)
- `RI`: Reactive Import
- `RE`: Reactive Export

**Generator Focus:**
- Initial implementation: `AI` (Active Import) only
- Future: Support multi-class generation

## Output Formats

### JSON Format

Structure follows the nested dictionary format described above.

```json
{
  "MPAN": "string",
  "site": "string",
  "MC": {
    "AI": {
      "YYYY-MM-DD": {
        "period_number": { ... }
      }
    }
  }
}
```

### CSV Format

Flattened structure for CSV output:

```csv
MPAN,Site,MeasurementClass,Date,Period,HHC,AEI,QtyId
1266448934017,Site A,AI,2024-01-01,1,2.5,A,kWh
1266448934017,Site A,AI,2024-01-01,2,2.3,A,kWh
...
```

## API Authentication

The generator does not need to handle authentication, but the output should be compatible with:
- **ApiKey**: Header-based API key authentication
- **ApiPassword**: Header-based password authentication

## Compatibility Requirements

### Version 2 Endpoints
- ✅ `/v2/mpanhhperperiod`: Primary target - HHPerPeriod format
- ✅ `/v2/mpanadditionaldetails`: Secondary target - EacAdditionalDetailsV2 format
- ✅ `/v1/filteredmpanhhbyperiod`: Tertiary target - YearlyHHByPeriodOutput format

### Response Types
- ✅ JSON (primary)
- ✅ CSV (secondary)
- ⏳ XML (future)

### Data Coverage
- **Minimum**: 1 meter, 30-minute intervals, 1 measurement class (AI)
- **Typical**: 1-1000 meters, mixed intervals, single or multi-class
- **Advanced**: Multiple measurement classes, estimated data flags

## Implementation Notes

### Period Number Calculation
```csharp
// For 30-minute intervals
int period = ((hour * 60 + minute) / 30) + 1;  // 1-48

// For 15-minute intervals
int period = ((hour * 60 + minute) / 15) + 1;  // 1-96
```

### Date Organization
All data organized by calendar date (YYYY-MM-DD), with all 48 or 96 periods per day included regardless of interval type.

### Multi-Meter Output
When generating for multiple meters:
- Each meter gets unique MPAN (derived from GUID)
- All meters in single JSON/CSV output file
- Consistent structure for all meters

### Deterministic vs Non-Deterministic

**Deterministic:**
- Same seed → identical MPAN generation
- All data marked as `A` (Actual)
- Reproducible consumption patterns

**Non-Deterministic:**
- Random MPAN generation
- Optional mix of `A` and `E` (Estimated) flags
- Varied consumption patterns

## Next Steps

1. Implement output formatters for HHPerPeriod JSON/CSV
2. Implement output formatters for EacAdditionalDetailsV2
3. Create data mappers from internal models to Electralink schemas
4. Add validation to ensure output matches API schema
5. Integration tests with Electralink API endpoints
