# Configuration Reference

Complete guide to configuring the IntervalGenerator API for different environments and use cases.

## Table of Contents

- [Environment Variables](#environment-variables)
- [appsettings.json Configuration](#appsettingsjson-configuration)
- [Business Profiles](#business-profiles)
- [Generation Parameters](#generation-parameters)
- [Performance Tuning](#performance-tuning)
- [Configuration Examples](#configuration-examples)

---

## Environment Variables

Environment variables override settings in `appsettings.json`. Use them for container/Kubernetes deployments.

### ASP.NET Core Settings

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Execution environment: `Development`, `Staging`, or `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` | URLs the API listens on (in Docker/containers) |
| `ASPNETCORE_HTTP_PORT` | `8080` | HTTP port for the API |

### API Settings

All API settings use the `ApiSettings__` prefix with nested configuration using `__` separators.

#### Authentication Settings

| Variable | Default | Description |
|----------|---------|-------------|
| `ApiSettings__Authentication__Enabled` | `true` | Enable/disable API key authentication |
| `ApiSettings__Authentication__ApiKey` | `demo-api-key` | API key for authentication |
| `ApiSettings__Authentication__ApiPassword` | `demo-api-password` | API password for authentication |

#### Meter Generation Settings

| Variable | Default | Description |
|----------|---------|-------------|
| `ApiSettings__MeterGeneration__DefaultMeterCount` | `100` | Number of meters to generate on startup (1-1000) |
| `ApiSettings__MeterGeneration__DefaultStartDate` | `2024-01-01` | Start date for generated data (YYYY-MM-DD) |
| `ApiSettings__MeterGeneration__DefaultEndDate` | `2024-12-31` | End date for generated data (YYYY-MM-DD) |
| `ApiSettings__MeterGeneration__DefaultIntervalPeriod` | `30` | Interval period in minutes: 5, 15, or 30 |
| `ApiSettings__MeterGeneration__DefaultBusinessType` | `Office` | Default business profile (see [Business Profiles](#business-profiles)) |
| `ApiSettings__MeterGeneration__DeterministicMode` | `true` | Enable deterministic mode for reproducible data |
| `ApiSettings__MeterGeneration__Seed` | `42` | Random seed for deterministic mode |
| `ApiSettings__MeterGeneration__EnableDynamicGeneration` | `true` | Allow on-demand generation for non-existent MPANs |

### Logging Settings

| Variable | Default | Description |
|----------|---------|-------------|
| `Logging__LogLevel__Default` | `Information` | Default log level: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None` |
| `Logging__LogLevel__Microsoft.AspNetCore` | `Warning` | Log level for ASP.NET Core framework logs |

### Setting Environment Variables

**In Docker:**

```bash
docker run -e ASPNETCORE_ENVIRONMENT=Development \
           -e ApiSettings__MeterGeneration__DefaultMeterCount=50 \
           intervalgenerator:latest
```

**In Kubernetes (via ConfigMap):**

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: interval-generator-config
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  ApiSettings__MeterGeneration__DefaultMeterCount: "100"
```

**In .NET development:**

```bash
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/IntervalGenerator.Api
```

---

## appsettings.json Configuration

Configuration can also be set in `appsettings.json` or environment-specific files like `appsettings.Development.json`.

### File Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
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
      "Seed": 42,
      "EnableDynamicGeneration": true
    }
  }
}
```

### Development Configuration (appsettings.Development.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  },
  "ApiSettings": {
    "Authentication": {
      "Enabled": false,
      "ApiKey": "dev-key",
      "ApiPassword": "dev-password"
    },
    "MeterGeneration": {
      "DefaultMeterCount": 10,
      "DeterministicMode": true,
      "Seed": 42,
      "EnableDynamicGeneration": true
    }
  }
}
```

---

## Business Profiles

IntervalGenerator includes five realistic consumption profiles that simulate different business types.

### Available Profiles

| Profile | Description | Usage Pattern | Peak Hours |
|---------|-------------|---------------|-----------|
| **Office** | Standard office environment | Weekday peaks, weekends low | 9 AM - 5 PM |
| **Manufacturing** | 24/7 industrial operation | Shift-based consistent load | 7 AM - 3 PM, 3 PM - 11 PM, 11 PM - 7 AM |
| **Retail** | Retail store/shopping | Extended hours with weekend activity | 10 AM - 8 PM (weekday), 10 AM - 9 PM (weekend) |
| **DataCenter** | High-availability computing | Consistent high load, minimal variation | 24/7 constant |
| **Educational** | School/university | Academic calendar patterns | Varies by semester |

### Profile Characteristics

**Office Profile:**
- Low consumption during nights
- Weekday peaks (8 AM - 6 PM)
- Weekends have minimal consumption
- Summer holidays show reduced load
- Realistic HVAC and lighting patterns

**Manufacturing Profile:**
- 24-hour operation
- Shift changes visible in load patterns
- Relatively stable with shift overlaps
- High baseline power consumption
- Consistent weekend usage

**Retail Profile:**
- Opening hours drive consumption (typically 10 AM - 9 PM)
- Weekend peaks (more shopper traffic)
- Reduced weekday morning consumption
- Seasonal variations (holidays, sales)
- Extended evening hours during peak seasons

**DataCenter Profile:**
- Near-constant high consumption
- Minimal daily variation
- Server cooling drives load
- No weekly or seasonal patterns
- Highest average consumption per meter

**Educational Profile:**
- Academic calendar patterns
- High consumption during school year (Sept-May)
- Reduced consumption during summer/breaks
- Weekday peaks (class times)
- Building-dependent variation

### Using Profiles in CLI

```bash
dotnet run --project src/IntervalGenerator.Cli -- generate \
  --profile Office \
  --start-date 2024-01-01 \
  --end-date 2024-01-31
```

---

## Generation Parameters

### Interval Periods

The interval period defines the granularity of generated data:

| Period | Minutes | Use Case |
|--------|---------|----------|
| **5** | 5 minutes | High-resolution analysis, detailed patterns |
| **15** | 15 minutes | Electralink standard, most common |
| **30** | 30 minutes | Reduced data volume, less granular |

**In Configuration:**

```json
"DefaultIntervalPeriod": 30
```

**In CLI:**

```bash
dotnet run --project src/IntervalGenerator.Cli -- generate --interval 15
```

### Date Ranges

Define the period for which data is generated:

```json
"DefaultStartDate": "2024-01-01",
"DefaultEndDate": "2024-12-31"
```

**Format**: `YYYY-MM-DD`

**In CLI:**

```bash
dotnet run --project src/IntervalGenerator.Cli -- generate \
  --start-date 2024-01-01 \
  --end-date 2024-12-31
```

### Meter Counts

Number of meters to generate:

| Count | Use Case |
|-------|----------|
| 1-10 | Testing, development |
| 10-100 | Small deployments, testing |
| 100-500 | Medium deployments |
| 500-1000 | Large deployments |

**Configuration:**

```json
"DefaultMeterCount": 100
```

**CLI:**

```bash
dotnet run --project src/IntervalGenerator.Cli -- generate --meters 500
```

### Deterministic vs Non-Deterministic Mode

**Deterministic Mode** - Reproducible results using a seed:

```json
"DeterministicMode": true,
"Seed": 42
```

Pros:
- Reproducible test data
- Consistent results across runs
- Good for regression testing

Cons:
- Data patterns repeat with same seed
- Less realistic variety

**Non-Deterministic Mode** - Random variation each run:

```json
"DeterministicMode": false
```

Pros:
- More realistic variation
- Different data each run
- Better for load testing

Cons:
- Results are not reproducible
- Harder to debug with different data

**CLI Usage:**

```bash
# Deterministic
dotnet run --project src/IntervalGenerator.Cli -- generate --seed 42

# Non-deterministic (omit seed)
dotnet run --project src/IntervalGenerator.Cli -- generate
```

### Measurement Classes

The API returns data with different measurement classes indicating the type of measurement:

| Class | Description | Example |
|-------|-------------|---------|
| **AI** | Active Import | Energy consumed by the customer |
| **AE** | Active Export | Energy fed back to grid (solar, etc.) |
| **RI** | Reactive Import | Reactive energy consumed |
| **RE** | Reactive Export | Reactive energy fed to grid |

Most configurations use only **AI** (Active Import) for standard consumption meters.

---

## Performance Tuning

### Memory Settings

**For Low-Load Environments (< 100 meters):**

```json
{
  "ApiSettings": {
    "MeterGeneration": {
      "DefaultMeterCount": 50,
      "DefaultIntervalPeriod": 30
    }
  }
}
```

In Kubernetes:
```yaml
resources:
  requests:
    memory: 128Mi
  limits:
    memory: 256Mi
```

**For High-Load Environments (> 500 meters):**

```json
{
  "ApiSettings": {
    "MeterGeneration": {
      "DefaultMeterCount": 1000,
      "DefaultIntervalPeriod": 15
    }
  }
}
```

In Kubernetes:
```yaml
resources:
  requests:
    memory: 512Mi
  limits:
    memory: 1Gi
```

### Meter Count Limits

Maximum meter count is 1000. Adjust based on:

- **Available memory**: Each meter uses ~1-2 MB depending on date range
- **Response time**: More meters = slower queries
- **Data generation time**: Larger batches take longer to generate

**Calculation:**

```
Approx Memory = MeterCount × DateRange(days) × IntervalPeriod × 0.01 MB
```

Example: 500 meters × 365 days × 30 minutes = ~180 MB

### Dynamic Generation Toggle

Enable for development (auto-generate MPANs on-demand):

```json
"EnableDynamicGeneration": true
```

Disable for production (only use pre-generated data):

```json
"EnableDynamicGeneration": false
```

---

## Configuration Examples

### Development Environment

**appsettings.Development.json:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "ApiSettings": {
    "Authentication": {
      "Enabled": false
    },
    "MeterGeneration": {
      "DefaultMeterCount": 10,
      "DefaultIntervalPeriod": 15,
      "DeterministicMode": true,
      "Seed": 42,
      "EnableDynamicGeneration": true
    }
  }
}
```

Run with:
```bash
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/IntervalGenerator.Api
```

### Testing Environment

**appsettings.Testing.json:**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "ApiSettings": {
    "Authentication": {
      "Enabled": true,
      "ApiKey": "test-key",
      "ApiPassword": "test-password"
    },
    "MeterGeneration": {
      "DefaultMeterCount": 100,
      "DefaultBusinessType": "Manufacturing",
      "DeterministicMode": true,
      "Seed": 12345,
      "EnableDynamicGeneration": false
    }
  }
}
```

### Production Environment

**Environment Variables in Kubernetes:**

```bash
kubectl create configmap interval-generator-config \
  --from-literal=ASPNETCORE_ENVIRONMENT=Production \
  --from-literal=Logging__LogLevel__Default=Warning \
  --from-literal=ApiSettings__MeterGeneration__DefaultMeterCount=500 \
  --from-literal=ApiSettings__MeterGeneration__DeterministicMode=false \
  --from-literal=ApiSettings__MeterGeneration__EnableDynamicGeneration=true

kubectl create secret generic interval-generator-secret \
  --from-literal=ApiSettings__Authentication__ApiKey='production-key-here' \
  --from-literal=ApiSettings__Authentication__ApiPassword='production-password-here'
```

### High-Availability Setup

For multiple replicas with consistent state:

```json
{
  "ApiSettings": {
    "MeterGeneration": {
      "DefaultMeterCount": 500,
      "DeterministicMode": true,
      "Seed": 42,
      "DefaultStartDate": "2024-01-01",
      "DefaultEndDate": "2024-12-31"
    },
    "Authentication": {
      "Enabled": true,
      "ApiKey": "secure-key",
      "ApiPassword": "secure-password"
    }
  }
}
```

Benefits:
- Deterministic mode ensures all replicas have identical data
- Faster startup (no random variation)
- Easier testing and debugging

---

## Configuration Precedence

Settings are loaded in this order (later overrides earlier):

1. `appsettings.json` (default)
2. `appsettings.{ASPNETCORE_ENVIRONMENT}.json` (dev-specific)
3. Environment variables

**Example:**

If both `appsettings.json` and `ApiSettings__MeterGeneration__DefaultMeterCount=200` environment variable are set, the environment variable value (200) will be used.

---

## Troubleshooting Configuration

### Issue: Configuration not taking effect

**Solution:**
- Verify environment variables are set correctly
- Check ConfigMap in Kubernetes: `kubectl describe configmap interval-generator-config`
- Ensure pods are restarted after ConfigMap changes
- Check logs for parse errors: `kubectl logs <pod-name>`

### Issue: Authentication failing

**Solution:**
- Verify `ApiSettings__Authentication__Enabled=true`
- Check API key and password are set in Secret: `kubectl describe secret interval-generator-secret`
- Ensure requests include correct headers:

```bash
curl -H "Api-Key: your-key" \
     -H "Api-Password: your-password" \
     http://api/endpoint
```

### Issue: Memory usage too high

**Solution:**
- Reduce `DefaultMeterCount` in configuration
- Shorten date range (`DefaultStartDate` and `DefaultEndDate`)
- Increase interval period (e.g., 30 instead of 15 minutes)
- Set resource limits in Kubernetes to trigger autoscaling
