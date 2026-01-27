# IntervalGenerator

A .NET-based smart meter interval data generator that produces realistic energy consumption data. Designed as a drop-in replacement for the Electralink EAC API for testing and development purposes.

## Features

- Generate 15-minute or 30-minute interval consumption data
- Multiple business profiles: Office, Manufacturing, Retail, Data Center, Educational
- Support for up to 1000 meters per generation run
- Deterministic mode (seeded) for reproducible test data
- Non-deterministic mode for realistic simulations
- Output formats: JSON (Electralink-compatible) and CSV
- REST API and CLI interfaces

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later

## Documentation

- **[Deployment Guide](docs/DEPLOYMENT.md)** - Deploy to Docker, Kubernetes, or with Garden.io
- **[Configuration Reference](docs/CONFIGURATION.md)** - Complete configuration guide and environment variables
- **[API Documentation](docs/API.md)** - REST API endpoint reference with examples
- **[Development Guide](docs/DEVELOPMENT.md)** - Local setup, building, testing, and contributing

## Quick Start

### CLI Usage

```bash
# Build the project
dotnet build

# Generate interval data (example)
dotnet run --project src/IntervalGenerator.Cli -- generate \
  --start-date 2024-01-01 \
  --end-date 2024-01-31 \
  --profile Office \
  --interval 30 \
  --output output.csv
```

### API Usage

```bash
# Run the API
dotnet run --project src/IntervalGenerator.Api

# The API will be available at http://localhost:5000
```

### Docker

```bash
# Build the Docker image
docker build -t interval-generator .

# Run the container
docker run -p 8080:8080 interval-generator
```

## Project Structure

```
src/
  IntervalGenerator.Core/       # Core generation engine and models
  IntervalGenerator.Profiles/   # Business consumption profiles
  IntervalGenerator.Output/     # Output formatters (CSV, JSON)
  IntervalGenerator.Api/        # REST API
  IntervalGenerator.Cli/        # Command-line interface

tests/
  IntervalGenerator.Core.Tests/
  IntervalGenerator.Profiles.Tests/
  IntervalGenerator.Api.Tests/
  IntervalGenerator.Integration.Tests/
```

## Business Profiles

The generator includes realistic consumption profiles for different business types:

| Profile | Description |
|---------|-------------|
| Office | Standard office hours pattern with weekday peaks |
| Manufacturing | 24/7 operation with shift-based patterns |
| Retail | Extended hours with weekend activity |
| DataCenter | Consistent high load with minimal variation |
| Educational | Academic calendar-based patterns |

## Configuration

### CLI Options

| Option | Description |
|--------|-------------|
| `--start-date` | Start date for generation (YYYY-MM-DD) |
| `--end-date` | End date for generation (YYYY-MM-DD) |
| `--profile` | Business profile (Office, Manufacturing, etc.) |
| `--interval` | Interval period in minutes (15 or 30) |
| `--meters` | Number of meters to generate (1-1000) |
| `--seed` | Random seed for deterministic output |
| `--output` | Output file path |
| `--format` | Output format (csv, json) |

### API Configuration

Configuration is managed via `appsettings.json`:

```json
{
  "ApiSettings": {
    "DefaultMeterCount": 10,
    "MaxMeterCount": 1000
  }
}
```

## Development

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Build for release
dotnet build -c Release
```

## Output Formats

### JSON (Electralink HHPerPeriod Compatible)

```json
{
  "mpan": "1234567890123",
  "measurementClass": "AI",
  "data": [
    {
      "date": "2024-01-01",
      "periods": [
        { "period": 1, "hhc": 12.34, "aei": "A" },
        { "period": 2, "hhc": 11.87, "aei": "A" }
      ]
    }
  ]
}
```

### CSV

```csv
MPAN,MeasurementClass,Date,Period,HHC,AEI
1234567890123,AI,2024-01-01,1,12.34,A
1234567890123,AI,2024-01-01,2,11.87,A
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Third-Party Licenses

This project uses several open-source libraries. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
