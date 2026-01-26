# CLI Project Build Errors - Out of Scope

## Overview
The `IntervalGenerator.Cli` project has pre-existing compilation errors that are **not related** to the code quality analyzer warning resolution initiative. These errors stem from a System.CommandLine API version incompatibility.

## Error Summary

### Total Errors: 31 (all in CLI project only)

### Error Categories

#### 1. Missing `InvokeAsync` Method (1 error)
**File**: `src/IntervalGenerator.Cli/Program.cs:9`
```
error CS1061: 'RootCommand' does not contain a definition for 'InvokeAsync'
```
**Cause**: The System.CommandLine NuGet package version used does not have the `InvokeAsync` method on `RootCommand`.

#### 2. Invalid Parameter Names (16 errors)
**File**: `src/IntervalGenerator.Cli/Commands/GenerateCommand.cs` (multiple lines)
```
error CS1739: The best overload for 'Option' does not have a parameter named 'description'
```
**Affected Lines**: 19, 27, 35, 51, 59, 75, 83, 90, 97, 113, 120, 128
**Cause**: The System.CommandLine API uses different parameter names in this version. The code uses `description` but the API may expect a different parameter name or signature.

#### 3. Missing `IsRequired` Property (12 errors)
**File**: `src/IntervalGenerator.Cli/Commands/GenerateCommand.cs` (multiple lines)
```
error CS0117: 'Option<T>' does not contain a definition for 'IsRequired'
```
**Affected Lines**: 22, 30, 38, 54, 62, 78, 85, 92, 100, 115, 123, 131
**Cause**: The `IsRequired` property doesn't exist on `Option<T>` in this version of System.CommandLine.

#### 4. Missing `AddValidator` Method (3 errors)
**File**: `src/IntervalGenerator.Cli/Commands/GenerateCommand.cs`
```
error CS1061: 'Option<int>' does not contain a definition for 'AddValidator'
```
**Affected Lines**: 40, 64, 102
**Cause**: The `AddValidator` extension method is not available in this version of System.CommandLine.

#### 5. Missing `InvocationContext` Type (1 error)
**File**: `src/IntervalGenerator.Cli/Commands/GenerateCommand.cs:150`
```
error CS0246: The type or namespace name 'InvocationContext' could not be found
```
**Cause**: The `InvocationContext` type is not available in the imported System.CommandLine version.

#### 6. Missing `SetHandler` Method (1 error)
**File**: `src/IntervalGenerator.Cli/Commands/GenerateCommand.cs:150`
```
error CS1061: 'Command' does not contain a definition for 'SetHandler'
```
**Cause**: The `SetHandler` method does not exist on `Command` in this version.

## Root Cause Analysis

The CLI project was built against an **older or incompatible version of System.CommandLine**. The current code uses the newer API, but the NuGet package version in the project file does not match.

## Resolution Options (For Future Work)

### Option 1: Update System.CommandLine Package (Recommended)
- Update the `System.CommandLine` NuGet package to the latest stable version
- Update the CLI code to use the current API

### Option 2: Revert CLI Code to Match Older API
- Update the CLI code to use the API from the installed System.CommandLine version
- This is less desirable as it limits functionality

### Option 3: Exclude CLI from Build
- If the CLI is not needed, add it to the build exclusion list
- This allows the project to build successfully for library/API use only

## Impact on Current Work

- ✓ All library code (Core, Profiles, Output, Api) builds successfully
- ✓ All test projects build successfully
- ✓ All 268 tests pass
- ✗ CLI project fails to compile (not used in tests or main API)

The CLI errors do **not** affect:
- The IntervalGenerator API functionality
- Any test execution
- The code quality analyzer warning resolution work

## Files Affected

- `src/IntervalGenerator.Cli/Program.cs`
- `src/IntervalGenerator.Cli/Commands/GenerateCommand.cs`

## Recommendations for Future Agents

1. **Do NOT attempt to fix CLI errors as part of analyzer warning resolution** - they are unrelated
2. **If fixing CLI is needed**, start by examining the System.CommandLine package version in the CLI `.csproj` file
3. **Check System.CommandLine documentation** for the correct API for the installed version
4. **Consider whether CLI functionality is essential** before investing effort in this upgrade

## References

- System.CommandLine NuGet: https://www.nuget.org/packages/System.CommandLine/
- Project file location: `src/IntervalGenerator.Cli/IntervalGenerator.Cli.csproj`
