# CLI Project - System.CommandLine Update

## Status: Resolved

The CLI project has been updated to use System.CommandLine 2.0.0-beta5+ API, resolving all previous compilation errors.

## Changes Made

### Framework Update
- Updated `System.CommandLine` package from `2.*` to `2.0.0-beta5.*`
- This version aligns with the path to System.CommandLine's stable release (expected November 2025 with .NET 10)

### API Migration
The CLI code was updated to use the new System.CommandLine 2.0.0-beta5+ API:

| Old Pattern (beta4) | New Pattern (beta5+) |
|---------------------|----------------------|
| `Option<T>("--name", description: "...")` | `Option<T>("--name") { Description = "..." }` |
| `option.SetDefaultValue(value)` | `option.DefaultValueFactory = _ => value` |
| `command.AddOption(option)` | `command.Options.Add(option)` |
| `command.SetHandler(...)` | `command.SetAction(...)` |
| `InvocationContext` | `ParseResult` (passed directly) |
| `rootCommand.Invoke(args)` | `rootCommand.Parse(args).Invoke()` |

### Key Benefits of beta5+
- **Simplified API**: Fewer types and concepts to learn
- **Smaller footprint**: 32% smaller library size
- **Faster parsing**: 12% performance improvement
- **Stable path**: Aligns with upcoming stable release

## CLI Usage

### Generate Command

```bash
# Basic usage - generate 7 days of data for a single office meter
interval-generator generate

# Specify date range and profile
interval-generator generate --start-date 2024-01-01 --end-date 2024-01-31 --profile Manufacturing

# Multiple meters with deterministic output
interval-generator generate --meters 10 --deterministic --seed 42

# Output to file in JSON format
interval-generator generate --output data.json --format json --pretty
```

### Options

| Option | Short | Default | Description |
|--------|-------|---------|-------------|
| `--start-date` | `-s` | 7 days ago | Start date for generation |
| `--end-date` | `-e` | Today | End date for generation |
| `--period` | `-p` | 30 | Interval period (5, 15, or 30 minutes) |
| `--profile` | `-t` | Office | Business type profile |
| `--meters` | `-m` | 1 | Number of meters (1-1000) |
| `--deterministic` | `-d` | false | Enable reproducible output |
| `--seed` | | null | Random seed for deterministic mode |
| `--output` | `-o` | stdout | Output file path |
| `--format` | `-f` | csv | Output format (csv, json) |
| `--site` | | null | Site name in output |
| `--pretty` | | false | Pretty-print JSON output |
| `--quiet` | `-q` | false | Suppress progress output |

### Available Profiles
- Office
- Manufacturing
- Retail
- DataCenter
- Educational

## References

- [System.CommandLine 2.0.0-beta5 Migration Guide](https://learn.microsoft.com/en-us/dotnet/standard/commandline/migration-guide-2.0.0-beta5)
- [Announcing System.CommandLine 2.0.0-beta5](https://github.com/dotnet/command-line-api/issues/2576)
