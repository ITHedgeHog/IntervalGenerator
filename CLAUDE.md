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

## Code Quality & Analyzer Warnings

### Policy: Resolve, Don't Suppress

Do not suppress code analyzer warnings via `.editorconfig`. Instead:

1. **Build regularly**: Run `dotnet build` after making changes to catch warnings early
2. **Resolve warnings**: Fix the underlying issues rather than disabling the analyzer
3. **Document exceptions**: Only suppress warnings if there's a documented, justified reason
4. **Justify suppressions**: Use inline `#pragma warning disable/restore` with comments explaining why

### Common Warnings to Address

**CA1304, CA1305, CA1310**: String comparisons without culture specification
- Use `StringComparison.OrdinalIgnoreCase` for case-insensitive comparisons
- Use `string.Equals()` or `.StartsWith()/.EndsWith()` with explicit `StringComparison`
- Example: `path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)`

**CA1507**: Use `nameof()` instead of magic strings
- Replace string literals with `nameof(propertyName)` where applicable
- Use for parameter validation and reflection

**CA1716**: Parameter names that match keywords
- Rename parameters like `next` to `nextDelegate` or similar
- Exception: established patterns like ASP.NET Core middleware use `_next` as a field

**CA1822**: Methods that could be static
- Refactor methods to be static if they don't use instance state

**CA1852**: Classes that could be sealed
- Seal concrete classes not designed for inheritance

### Workflow

When analyzer warnings appear:
1. Review the warning and understand why it's triggered
2. Implement the fix in the source code
3. Run `dotnet build` to verify the warning is resolved
4. Commit the fix with a clear message

Never use `.editorconfig` suppression as a shortcut for addressing code quality issues.
