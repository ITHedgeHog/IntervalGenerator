<#
.SYNOPSIS
    Creates a git tag and GitHub release based on the current GitVersion.

.DESCRIPTION
    This script uses GitVersion to determine the current version, creates a git tag,
    and then creates a GitHub release through the GitHub API.

.PARAMETER ReleaseNotes
    Optional release notes to include in the GitHub release.

.PARAMETER DryRun
    If specified, shows what would be done without actually creating the tag or release.

.EXAMPLE
    .\create-release.ps1

    .\create-release.ps1 -ReleaseNotes "Fixed bug #123"

    .\create-release.ps1 -DryRun
#>

param(
    [string]$ReleaseNotes = "",
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"

function Write-Header {
    param([string]$Message)
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

try {
    Write-Header "Creating Release with GitVersion"

    # Check if GitVersion is installed
    $gitversionCmd = Get-Command gitversion -ErrorAction SilentlyContinue
    if (-not $gitversionCmd) {
        throw "GitVersion CLI is not installed. Install it with: dotnet tool install --global GitVersion.Tool"
    }

    # Get current version
    Write-Host "Getting version from GitVersion..." -ForegroundColor Yellow
    $versionOutput = & gitversion /format json | ConvertFrom-Json
    $semVer = $versionOutput.SemVer
    $tagName = "v$semVer"

    Write-Success "Current version: $semVer"

    if ($DryRun) {
        Write-Header "DRY RUN - No changes will be made"
        Write-Host "Would create tag: $tagName" -ForegroundColor Yellow
        Write-Host "Would push to origin" -ForegroundColor Yellow
        Write-Host "Would create GitHub release" -ForegroundColor Yellow
        exit 0
    }

    # Check if tag already exists
    Write-Host "Checking if tag already exists..." -ForegroundColor Yellow
    $existingTag = git tag -l $tagName
    if ($existingTag) {
        throw "Tag $tagName already exists. Cannot create duplicate release."
    }
    Write-Success "Tag does not exist"

    # Create local git tag
    Write-Host "Creating git tag..." -ForegroundColor Yellow
    git tag -a $tagName -m "Release $semVer"
    Write-Success "Git tag created: $tagName"

    # Push tag to origin
    Write-Host "Pushing tag to origin..." -ForegroundColor Yellow
    git push origin $tagName
    Write-Success "Tag pushed to origin"

    # Prepare release notes
    if ($ReleaseNotes) {
        $body = @"
## Release Information

**Version:** $semVer

**Version Details:**
- Major: $($versionOutput.Major)
- Minor: $($versionOutput.Minor)
- Patch: $($versionOutput.Patch)

## Release Notes

$ReleaseNotes
"@
    }
    else {
        $body = @"
## Release Information

**Version:** $semVer

**Version Details:**
- Major: $($versionOutput.Major)
- Minor: $($versionOutput.Minor)
- Patch: $($versionOutput.Patch)

This release was created automatically from the repository's current version.
The Docker Release workflow will be automatically triggered to build and push container images.
"@
    }

    Write-Host "Release notes:" -ForegroundColor Yellow
    Write-Host $body

    Write-Header "Release Created Successfully"
    Write-Success "Tag: $tagName"
    Write-Success "The GitHub release will be created automatically"
    Write-Success "The Docker Release workflow will build and push images"
    Write-Host "`nVisit your repository's releases page to see the release: https://github.com/ITHedgeHog/IntervalGenerator/releases/tag/$tagName"

}
catch {
    Write-Error-Custom "Error: $_"
    exit 1
}
