# API Documentation

REST API reference for IntervalGenerator - a mock Electralink EAC API for testing and development.

## Table of Contents

- [API Overview](#api-overview)
- [Authentication](#authentication)
- [Endpoints](#endpoints)
- [Request/Response Examples](#requestresponse-examples)
- [Error Handling](#error-handling)

---

## API Overview

### Purpose

IntervalGenerator provides a mock implementation of the Electralink EAC (Energy Account Code) API for development and testing purposes. It generates realistic smart meter interval data and serves it in Electralink-compatible formats.

### Base URL

```
http://localhost:8080
```

In production or Kubernetes:
```
http://interval-generator-service
```

### Port Information

- **Container Port**: 8080
- **Kubernetes Service Port**: 80 (maps to container port 8080)

### Response Formats

- **JSON** (default) - Electralink-compatible JSON structure
- **CSV** - Comma-separated values format

Set response format via `response-type` header:

```bash
# JSON (default)
curl http://localhost:8080/endpoint

# CSV
curl -H "response-type: csv" http://localhost:8080/endpoint
```

---

## Authentication

### Overview

The API supports header-based API key authentication. Authentication is enabled by default but can be disabled for development.

### Required Headers

When authentication is enabled, all requests must include:

```
Api-Key: <api-key>
Api-Password: <api-password>
```

### Default Credentials

Default development credentials (if unchanged):

```
Api-Key: demo-api-key
Api-Password: demo-api-password
```

### Example with Authentication

```bash
curl -H "Api-Key: demo-api-key" \
     -H "Api-Password: demo-api-password" \
     http://localhost:8080/v2/mpanhhperperiod?mpan=1234567890123
```

### Disabling Authentication

For development/testing, disable authentication:

**Via environment variable:**

```bash
export ApiSettings__Authentication__Enabled=false
```

**Via appsettings.json:**

```json
{
  "ApiSettings": {
    "Authentication": {
      "Enabled": false
    }
  }
}
```

### Changing Credentials

**Via environment variables:**

```bash
export ApiSettings__Authentication__ApiKey=your-custom-key
export ApiSettings__Authentication__ApiPassword=your-custom-password
```

**Via appsettings.json:**

```json
{
  "ApiSettings": {
    "Authentication": {
      "Enabled": true,
      "ApiKey": "your-custom-key",
      "ApiPassword": "your-custom-password"
    }
  }
}
```

---

## Endpoints

### GET /health

Health check endpoint. No authentication required.

**Purpose**: Verify the API is running and healthy.

**Request:**

```bash
curl http://localhost:8080/health
```

**Response (200 OK):**

```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

### GET /

API information endpoint. No authentication required.

**Purpose**: Get API metadata and available endpoints.

**Request:**

```bash
curl http://localhost:8080/
```

**Response (200 OK):**

```json
{
  "name": "Electralink EAC API (Mock)",
  "version": "1.0.0",
  "metersLoaded": 100,
  "endpoints": [
    "/v2/mpanhhperperiod?mpan={mpan}",
    "/v2/mpanadditionaldetails?mpan={mpan}",
    "/v1/filteredmpanhhbyperiod?mpan={mpan}&StartDate={date}&EndDate={date}"
  ]
}
```

---

### GET /mpans

List all available MPANs. No authentication required.

**Purpose**: Discover available Meter Point Administration Numbers.

**Request:**

```bash
curl http://localhost:8080/mpans
```

**Response (200 OK):**

```json
{
  "count": 100,
  "mpans": [
    "1234567890123",
    "1234567890124",
    "1234567890125"
  ]
}
```

---

### GET /v2/mpanhhperperiod

Retrieve half-hourly consumption data organized by measurement class and period.

**Purpose**: Get detailed consumption data for a specific meter.

**Authentication**: Required

**Query Parameters:**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `mpan` | Yes | String | Meter Point Administration Number |
| `response-type` | No | String | Response format: `json` (default) or `csv` |

**Request:**

```bash
curl -H "Api-Key: demo-api-key" \
     -H "Api-Password: demo-api-password" \
     "http://localhost:8080/v2/mpanhhperperiod?mpan=1234567890123"
```

**Response (200 OK - JSON):**

```json
{
  "MPAN": "1234567890123",
  "site": "Office Site 1",
  "MC": {
    "AI": {
      "2024-01-01": {
        "1": {
          "period": 1,
          "hhc": 12.34,
          "aei": "A",
          "qty_id": "kWh"
        },
        "2": {
          "period": 2,
          "hhc": 11.87,
          "aei": "A",
          "qty_id": "kWh"
        }
      },
      "2024-01-02": {
        "1": {
          "period": 1,
          "hhc": 13.45,
          "aei": "A",
          "qty_id": "kWh"
        }
      }
    }
  }
}
```

**Response (200 OK - CSV):**

```csv
MPAN,Site,MeasurementClass,Date,Period,HHC,AEI,QtyId
1234567890123,Office Site 1,AI,2024-01-01,1,12.34,A,kWh
1234567890123,Office Site 1,AI,2024-01-01,2,11.87,A,kWh
1234567890123,Office Site 1,AI,2024-01-02,1,13.45,A,kWh
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `MPAN` | String | Meter Point Administration Number |
| `site` | String | Site/location name |
| `MC` | Object | Data grouped by Measurement Class |
| `MC.{class}.{date}.{period}` | Object | Period data |
| `hhc` | Decimal | Half-hourly consumption in kWh |
| `aei` | String | Data quality flag: A=Actual, E=Estimated, M=Missing, X=Corrected |
| `qty_id` | String | Unit identifier (typically "kWh") |

---

### GET /v2/mpanadditionaldetails

Retrieve additional metadata about a meter.

**Purpose**: Get meter details like site name and classification.

**Authentication**: Required

**Query Parameters:**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `mpan` | Yes | String | Meter Point Administration Number |

**Request:**

```bash
curl -H "Api-Key: demo-api-key" \
     -H "Api-Password: demo-api-password" \
     "http://localhost:8080/v2/mpanadditionaldetails?mpan=1234567890123"
```

**Response (200 OK):**

```json
{
  "mpan": "1234567890123",
  "siteName": "Office Site 1",
  "siteAddress": "123 Main St, City",
  "businessType": "Office",
  "meterSerialNumber": "12345678",
  "meterInstallDate": "2023-01-01"
}
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `mpan` | String | Meter Point Administration Number |
| `siteName` | String | Name of the site |
| `siteAddress` | String | Physical address of the meter |
| `businessType` | String | Business profile (Office, Manufacturing, etc.) |
| `meterSerialNumber` | String | Serial number of the meter |
| `meterInstallDate` | String | Installation date (ISO 8601) |

---

### GET /v1/filteredmpanhhbyperiod

Retrieve filtered half-hourly data by date range and measurement class.

**Purpose**: Get consumption data filtered by specific date range and measurement class.

**Authentication**: Required

**Query Parameters:**

| Parameter | Required | Type | Description |
|-----------|----------|------|-------------|
| `mpan` | Yes | String | Meter Point Administration Number |
| `StartDate` | No | String | Start date (ISO 8601: YYYY-MM-DD) |
| `EndDate` | No | String | End date (ISO 8601: YYYY-MM-DD) |
| `MeasurementClass` | No | String | Filter by measurement class (AI, AE, RI, RE) |
| `response-type` | No | String | Response format: `json` (default) or `csv` |

**Request:**

```bash
curl -H "Api-Key: demo-api-key" \
     -H "Api-Password: demo-api-password" \
     "http://localhost:8080/v1/filteredmpanhhbyperiod?mpan=1234567890123&StartDate=2024-01-01&EndDate=2024-01-31&MeasurementClass=AI"
```

**Response (200 OK - JSON):**

```json
{
  "mpan": "1234567890123",
  "startDate": "2024-01-01",
  "endDate": "2024-01-31",
  "daysActual": 31,
  "daysEstimated": 0,
  "daysMissing": 0,
  "aiYearlyValue": 744.32,
  "actualMeasurements": [
    {
      "date": "2024-01-01",
      "qtyId": "kWh",
      "periods": [
        {
          "period": 1,
          "hhc": 12.34,
          "aei": "A"
        },
        {
          "period": 2,
          "hhc": 11.87,
          "aei": "A"
        }
      ]
    }
  ],
  "estimatedMeasurements": [],
  "missingMeasurement": []
}
```

**Response (200 OK - CSV):**

```csv
MPAN,Date,Period,HHC,AEI,QtyId
1234567890123,2024-01-01,1,12.34,A,kWh
1234567890123,2024-01-01,2,11.87,A,kWh
1234567890123,2024-01-02,1,13.45,A,kWh
```

**Response Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `mpan` | String | Meter Point Administration Number |
| `startDate` | String | Filtered start date |
| `endDate` | String | Filtered end date |
| `daysActual` | Integer | Number of days with actual (verified) readings |
| `daysEstimated` | Integer | Number of days with estimated readings |
| `daysMissing` | Integer | Number of days with missing data |
| `aiYearlyValue` | Decimal | Total Active Import consumption in period |
| `actualMeasurements` | Array | Array of day measurements with actual readings |
| `estimatedMeasurements` | Array | Array of day measurements with estimated readings |
| `missingMeasurement` | Array | Array of day measurements with missing data |

---

## Request/Response Examples

### Complete Workflow Example

**1. Verify API is running:**

```bash
curl http://localhost:8080/health
```

**2. Get available MPANs:**

```bash
curl http://localhost:8080/mpans
```

Response: List of 100 generated MPAN numbers

**3. Get API info:**

```bash
curl http://localhost:8080/
```

**4. Fetch consumption data for first MPAN (JSON):**

```bash
curl -H "Api-Key: demo-api-key" \
     -H "Api-Password: demo-api-password" \
     "http://localhost:8080/v2/mpanhhperperiod?mpan=1234567890123"
```

**5. Fetch as CSV:**

```bash
curl -H "Api-Key: demo-api-key" \
     -H "Api-Password: demo-api-password" \
     -H "response-type: csv" \
     "http://localhost:8080/v2/mpanhhperperiod?mpan=1234567890123" \
     > consumption.csv
```

**6. Get meter metadata:**

```bash
curl -H "Api-Key: demo-api-key" \
     -H "Api-Password: demo-api-password" \
     "http://localhost:8080/v2/mpanadditionaldetails?mpan=1234567890123"
```

**7. Filter data by date range:**

```bash
curl -H "Api-Key: demo-api-key" \
     -H "Api-Password: demo-api-password" \
     "http://localhost:8080/v1/filteredmpanhhbyperiod?mpan=1234567890123&StartDate=2024-01-01&EndDate=2024-01-31"
```

### Python Example

```python
import requests
import json

BASE_URL = "http://localhost:8080"
HEADERS = {
    "Api-Key": "demo-api-key",
    "Api-Password": "demo-api-password"
}

# Get available MPANs
response = requests.get(f"{BASE_URL}/mpans")
mpans = response.json()["mpans"]
print(f"Available MPANs: {len(mpans)}")

# Get data for first MPAN
mpan = mpans[0]
response = requests.get(
    f"{BASE_URL}/v2/mpanhhperperiod",
    params={"mpan": mpan},
    headers=HEADERS
)
data = response.json()
print(f"Data for {mpan}:")
print(json.dumps(data, indent=2))

# Get as CSV
response = requests.get(
    f"{BASE_URL}/v2/mpanhhperperiod",
    params={"mpan": mpan},
    headers={**HEADERS, "response-type": "csv"}
)
print(response.text)
```

### JavaScript/Node.js Example

```javascript
const BASE_URL = "http://localhost:8080";
const headers = {
  "Api-Key": "demo-api-key",
  "Api-Password": "demo-api-password"
};

// Get available MPANs
const mpansResponse = await fetch(`${BASE_URL}/mpans`);
const { mpans } = await mpansResponse.json();
console.log(`Available MPANs: ${mpans.length}`);

// Get consumption data
const mpan = mpans[0];
const dataResponse = await fetch(
  `${BASE_URL}/v2/mpanhhperperiod?mpan=${mpan}`,
  { headers }
);
const data = await dataResponse.json();
console.log(`Data for ${mpan}:`, data);

// Get as CSV
const csvResponse = await fetch(
  `${BASE_URL}/v2/mpanhhperperiod?mpan=${mpan}`,
  {
    headers: {
      ...headers,
      "response-type": "csv"
    }
  }
);
const csv = await csvResponse.text();
console.log(csv);
```

---

## Error Handling

### HTTP Status Codes

| Status | Meaning | Scenario |
|--------|---------|----------|
| 200 | OK | Request successful |
| 400 | Bad Request | Missing required parameter or invalid format |
| 401 | Unauthorized | Missing or invalid authentication headers |
| 404 | Not Found | MPAN doesn't exist and dynamic generation disabled |
| 500 | Internal Server Error | Unexpected server error |

### Error Response Format

```json
{
  "error": "Unauthorized",
  "message": "Invalid API credentials",
  "status": 401
}
```

### Common Errors

**Missing Authentication:**

```
Status: 401
{
  "error": "Unauthorized",
  "message": "Missing or invalid API key",
  "status": 401
}
```

**Missing MPAN Parameter:**

```
Status: 400
{
  "error": "Bad Request",
  "message": "MPAN parameter is required",
  "status": 400
}
```

**MPAN Not Found:**

```
Status: 404
{
  "error": "Not Found",
  "message": "MPAN 9999999999999 not found",
  "status": 404
}
```

---

## Measurement Classes

The API supports different measurement classes representing types of energy flow:

| Class | Full Name | Description | Example |
|-------|-----------|-------------|---------|
| AI | Active Import | Energy consumed (metered consumption) | Standard meter |
| AE | Active Export | Energy generated and exported (solar) | PV system |
| RI | Reactive Import | Reactive power consumed | Industrial load |
| RE | Reactive Export | Reactive power generated | Generator |

Most generated data uses **AI** (Active Import) for standard consumption meters.

---

## Data Quality Flags

The `aei` field indicates data quality:

| Flag | Meaning | Description |
|------|---------|-------------|
| A | Actual | Verified meter reading |
| E | Estimated | Calculated or estimated value |
| M | Missing | No data available for period |
| X | Corrected | Corrected reading after verification |

---

## Performance Considerations

### Response Size

Large date ranges and many meters result in large responses:

```
Response size ≈ MeterCount × DateRangeInDays × PeriodsPerDay × 100 bytes
```

Example: 1 meter × 365 days × 48 periods (30-min) × 100 bytes ≈ 1.7 MB

For large exports, consider:
- Using CSV format (smaller file size)
- Filtering by date range
- Processing results in chunks

### Rate Limiting

Currently no rate limiting is implemented. For production use, consider adding rate limiting via proxy or API gateway.

---

## Troubleshooting

### Connection Refused

**Issue**: `curl: (7) Failed to connect`

**Solution**:
- Verify API is running: `curl http://localhost:8080/health`
- Check port is correct (default 8080)
- In Kubernetes: verify pod is running: `kubectl get pods`
- Check service: `kubectl get svc interval-generator-service`

### Authentication Failures

**Issue**: 401 Unauthorized

**Solution**:
- Include both `Api-Key` and `Api-Password` headers
- Verify credentials match configuration
- Check if authentication is enabled: `ApiSettings__Authentication__Enabled=true`

### MPAN Not Found

**Issue**: 404 Not Found

**Solution**:
- Verify MPAN exists: `curl http://localhost:8080/mpans`
- Enable dynamic generation: `ApiSettings__MeterGeneration__EnableDynamicGeneration=true`
- Pre-load data on startup with appropriate `DefaultMeterCount`
