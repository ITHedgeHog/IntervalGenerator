# IntervalGenerator

## Project Overview
This is a .NET 10 project. The codebase uses modern C# features and follows Microsoft's recommended practices.

## Build & Run
```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the project
dotnet run

# Run tests
dotnet test

# Build for release
dotnet build -c Release
```

## Code Style
- Follow the `.editorconfig` rules for formatting
- Use file-scoped namespaces
- Use primary constructors where appropriate
- Prefer `var` when the type is apparent
- Use nullable reference types (enabled project-wide)

## Project Structure
```
/src          - Source projects
/tests        - Test projects
```

## Dependencies
- .NET 10 SDK (see `global.json` for exact version)

## Testing
Run all tests with `dotnet test`. Ensure all tests pass before committing.
