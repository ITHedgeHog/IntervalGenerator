# Development Guide

Setup and contributing guide for IntervalGenerator project development.

## Table of Contents

- [Local Development Setup](#local-development-setup)
- [Building](#building)
- [Testing](#testing)
- [Contributing](#contributing)

---

## Local Development Setup

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) or later
- Git
- A text editor or IDE (Visual Studio, VS Code, Rider)

### Clone Repository

```bash
git clone https://github.com/yourusername/IntervalGenerator.git
cd IntervalGenerator
```

### Restore Dependencies

```bash
dotnet restore
```

This restores all NuGet dependencies specified in the project files.

### Running the API Locally

```bash
dotnet run --project src/IntervalGenerator.Api
```

The API will start on `http://localhost:5000` (or check console for actual port)

### Running the CLI

```bash
dotnet run --project src/IntervalGenerator.Cli -- generate \
  --start-date 2024-01-01 \
  --end-date 2024-01-31 \
  --profile Office \
  --interval 30 \
  --output output.csv
```

### Development Configuration

Create `src/IntervalGenerator.Api/appsettings.Development.json` for local overrides:

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
      "DeterministicMode": true,
      "Seed": 42
    }
  }
}
```

Run in development mode:

```bash
set ASPNETCORE_ENVIRONMENT=Development
dotnet run --project src/IntervalGenerator.Api
```

### Dev Container Option

For a containerized development environment:

```bash
# Build dev container
docker build -t interval-generator-dev -f Dockerfile.dev .

# Run dev container
docker run -it -v ${PWD}:/src interval-generator-dev
```

Inside container:

```bash
cd /src
dotnet restore
dotnet run --project src/IntervalGenerator.Api
```

---

## Building

### Build Debug Configuration

```bash
dotnet build
```

Output directory: `src/IntervalGenerator.Api/bin/Debug/net10.0/`

### Build Release Configuration

```bash
dotnet build -c Release
```

Output directory: `src/IntervalGenerator.Api/bin/Release/net10.0/`

### Publish for Deployment

```bash
dotnet publish -c Release -o ./publish
```

Publish output can be deployed to any machine with the .NET runtime.

### Docker Build

Build Docker image:

```bash
docker build -t intervalgenerator:latest .
```

Build with specific version:

```bash
docker build -t intervalgenerator:v1.0.0 --build-arg VERSION=v1.0.0 .
```

---

## Testing

### Run All Tests

```bash
dotnet test
```

### Run Tests in Specific Project

```bash
dotnet test tests/IntervalGenerator.Core.Tests
dotnet test tests/IntervalGenerator.Api.Tests
```

### Run Tests with Coverage

```bash
dotnet test /p:CollectCoverage=true
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~IntervalGenerator.Core.Tests.MeterGenerationTests.GenerateOfficeConsumption"
```

### Run Tests with Logging

```bash
dotnet test --logger "console;verbosity=detailed"
```

### Watch Mode (Continuous Testing)

Some IDEs support watch mode. In VS Code with the C# extension:

```
Ctrl+Shift+P -> "Dotnet: Watch"
```

Or from CLI with dotnet-watch:

```bash
dotnet tool install -g dotnet-watch
dotnet watch test
```

### Test Structure

Tests are organized by project:

```
tests/
  IntervalGenerator.Core.Tests/        # Core generation engine
  IntervalGenerator.Profiles.Tests/    # Business profile logic
  IntervalGenerator.Api.Tests/         # API endpoint tests
  IntervalGenerator.Integration.Tests/ # End-to-end tests
```

### Writing Tests

Test template:

```csharp
using Xunit;
using IntervalGenerator.Core.Models;
using IntervalGenerator.Core.Services;

namespace IntervalGenerator.Core.Tests;

public class MeterGenerationTests
{
    [Fact]
    public void GenerateMeters_WithValidInput_ReturnsExpectedCount()
    {
        // Arrange
        var config = new GenerationConfiguration
        {
            MeterCount = 10,
            BusinessType = "Office",
            Deterministic = true,
            Seed = 42
        };
        var generator = new MeterGenerator();

        // Act
        var meters = generator.Generate(config);

        // Assert
        Assert.Equal(10, meters.Count);
    }
}
```

---

## Contributing

### Code Style

Follow the `.editorconfig` rules automatically enforced by the IDE.

Key conventions:

- **Namespaces**: File-scoped namespaces

```csharp
namespace IntervalGenerator.Core.Models;

public class MyClass
{
}
```

- **var keyword**: Use `var` when type is obvious

```csharp
var config = new GenerationConfiguration();    // Good
var result = GetMeterData();                    // Good if return type is clear
```

- **Nullable reference types**: Enabled project-wide

```csharp
public string GetName() => "Name";              // Non-nullable
public string? GetOptionalName() => null;       // Nullable
```

- **Primary constructors**: Use where appropriate

```csharp
public class MeterGenerator(ILogger<MeterGenerator> logger)
{
    public void Generate() => logger.LogInformation("Generating");
}
```

### Code Quality - Analyzer Warnings

Policy: **Resolve, don't suppress**

1. Build regularly to catch warnings early: `dotnet build`
2. Fix the underlying issue rather than suppressing
3. Only suppress with documented reason via inline `#pragma`

**Common Warnings:**

**CA1304/CA1305/CA1310** - String comparisons without culture:

```csharp
// Bad
if (value.StartsWith("prefix"))

// Good
if (value.StartsWith("prefix", StringComparison.OrdinalIgnoreCase))
```

**CA1507** - Use `nameof()` instead of strings:

```csharp
// Bad
if (param == null) throw new ArgumentNullException("param");

// Good
if (param == null) throw new ArgumentNullException(nameof(param));
```

**CA1716** - Parameter names that are keywords:

```csharp
// Bad
public void Process(int next)

// Good
public void Process(int nextValue)
```

**CA1822** - Methods that could be static:

```csharp
// Make static if doesn't use instance state
public static int Calculate() => 42;
```

### Git Workflow

1. **Create feature branch**:

```bash
git checkout -b feature/my-feature
```

2. **Make changes and build**:

```bash
dotnet build
dotnet test
```

3. **Commit with clear message**:

```bash
git add .
git commit -m "Add feature: description of changes"
```

4. **Push and create PR**:

```bash
git push origin feature/my-feature
```

### Pull Request Process

1. **Ensure tests pass**: `dotnet test`
2. **Fix analyzer warnings**: `dotnet build`
3. **Update documentation** if necessary
4. **Keep commits clean**: Squash if needed
5. **Provide clear PR description**:

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Refactoring
- [ ] Documentation

## Testing
- Tested with X meters and Y date range
- Verified CSV and JSON output

## Checklist
- [ ] Code follows style guidelines
- [ ] All tests pass
- [ ] Documentation updated
- [ ] No new analyzer warnings
```

### Code Review Expectations

- Code must pass `dotnet build` without warnings
- All tests must pass with `dotnet test`
- Changes should follow the established patterns
- Documentation should be updated if behavior changes

---

## Project Structure

```
IntervalGenerator/
├── src/
│   ├── IntervalGenerator.Core/          # Core generation engine
│   │   ├── Models/                      # Data models
│   │   ├── Services/                    # Generation services
│   │   └── Profiles/                    # Business profiles
│   ├── IntervalGenerator.Api/           # REST API
│   │   ├── Endpoints/                   # API endpoint handlers
│   │   ├── Models/                      # Request/response models
│   │   ├── Authentication/              # Auth middleware
│   │   └── Program.cs                   # Startup configuration
│   ├── IntervalGenerator.Cli/           # Command-line interface
│   ├── IntervalGenerator.Profiles/      # Profile implementations
│   └── IntervalGenerator.Output/        # Output formatters
├── tests/
│   ├── IntervalGenerator.Core.Tests/
│   ├── IntervalGenerator.Api.Tests/
│   └── IntervalGenerator.Integration.Tests/
├── k8s/                                 # Kubernetes manifests
├── docs/                                # Documentation
├── Dockerfile
├── garden.yml
└── README.md
```

### Key Files

- `.editorconfig` - Code style rules
- `Directory.Build.props` - Shared build properties
- `global.json` - .NET SDK version
- `IntervalGenerator.sln` - Solution file

---

## Common Tasks

### Adding a New Business Profile

1. Create a new class inheriting from `IBusinessProfile` in `src/IntervalGenerator.Profiles/`
2. Implement consumption generation logic
3. Add tests in `tests/IntervalGenerator.Profiles.Tests/`
4. Register in profile factory
5. Update documentation in `docs/CONFIGURATION.md`

### Adding an API Endpoint

1. Create new endpoint handler in `src/IntervalGenerator.Api/Endpoints/`
2. Map endpoint in `Program.cs`
3. Add tests in `tests/IntervalGenerator.Api.Tests/`
4. Update API documentation in `docs/API.md`

### Updating Configuration

1. Update `ApiSettings` classes in `src/IntervalGenerator.Api/Models/`
2. Update default `appsettings.json`
3. Update `docs/CONFIGURATION.md`
4. Update Kubernetes ConfigMap in `k8s/configmap.yaml`

---

## Debugging

### Debug in Visual Studio Code

1. Install C# extension
2. Set breakpoints in code
3. Press `F5` or use Debug menu
4. Breakpoints will hit when code executes

### Debug with Logging

Enable debug logging:

```bash
set Logging__LogLevel__Default=Debug
dotnet run --project src/IntervalGenerator.Api
```

Or in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### Debug CLI

```bash
dotnet run --project src/IntervalGenerator.Cli -- generate --help
```

---

## Performance Profiling

### Memory Usage

```bash
dotnet build -c Release
dotnet IntervalGenerator.Api.dll
```

Monitor with Task Manager or `dotnet-counters`:

```bash
dotnet tool install -g dotnet-counters
dotnet-counters monitor -p <process-id>
```

### Benchmarking

Install benchmarking tool:

```bash
dotnet tool install -g BenchmarkDotNet.Tool
```

Run benchmarks:

```bash
dotnet bench
```

---

## Helpful Commands

| Command | Purpose |
|---------|---------|
| `dotnet build` | Build solution |
| `dotnet test` | Run all tests |
| `dotnet run --project src/IntervalGenerator.Api` | Run API locally |
| `dotnet run --project src/IntervalGenerator.Cli -- generate --help` | CLI help |
| `dotnet publish -c Release` | Publish for deployment |
| `dotnet format` | Format code (if installed) |
| `dotnet clean` | Clean build artifacts |

---

## Resources

- [.NET Documentation](https://learn.microsoft.com/dotnet/)
- [ASP.NET Core Documentation](https://learn.microsoft.com/aspnet/core/)
- [C# Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/)
- [EditorConfig Support](https://editorconfig.org/)

---

## Getting Help

- Check existing issues and documentation
- Ask questions in discussions
- Report bugs with detailed reproduction steps
- Suggest improvements with use cases
