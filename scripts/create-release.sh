#!/bin/bash

# Create a git tag and GitHub release based on the current GitVersion
#
# Usage:
#   ./create-release.sh                    # Create release with no additional notes
#   ./create-release.sh "Release notes"    # Create release with notes
#   ./create-release.sh --dry-run          # Show what would be done

set -e

DRY_RUN=false
RELEASE_NOTES=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        *)
            RELEASE_NOTES="$1"
            shift
            ;;
    esac
done

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Helper functions
print_header() {
    echo -e "\n${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${CYAN}$1${NC}"
    echo -e "${CYAN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
}

print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

print_error() {
    echo -e "${RED}✗ $1${NC}"
}

print_info() {
    echo -e "${YELLOW}$1${NC}"
}

# Check if GitVersion is installed
if ! command -v gitversion &> /dev/null; then
    print_error "GitVersion CLI is not installed. Install it with: dotnet tool install --global GitVersion.Tool"
    exit 1
fi

print_header "Creating Release with GitVersion"

# Get current version
print_info "Getting version from GitVersion..."
VERSION_JSON=$(gitversion /format json)
SEM_VER=$(echo "$VERSION_JSON" | grep -o '"SemVer":"[^"]*' | cut -d'"' -f4)
MAJOR=$(echo "$VERSION_JSON" | grep -o '"Major":[0-9]*' | cut -d':' -f2)
MINOR=$(echo "$VERSION_JSON" | grep -o '"Minor":[0-9]*' | cut -d':' -f2)
PATCH=$(echo "$VERSION_JSON" | grep -o '"Patch":[0-9]*' | cut -d':' -f2)
TAG_NAME="v$SEM_VER"

print_success "Current version: $SEM_VER"

if [ "$DRY_RUN" = true ]; then
    print_header "DRY RUN - No changes will be made"
    print_info "Would create tag: $TAG_NAME"
    print_info "Would push to origin"
    print_info "Would create GitHub release"
    exit 0
fi

# Check if tag already exists
print_info "Checking if tag already exists..."
if git rev-parse "$TAG_NAME" >/dev/null 2>&1; then
    print_error "Tag $TAG_NAME already exists. Cannot create duplicate release."
    exit 1
fi
print_success "Tag does not exist"

# Create local git tag
print_info "Creating git tag..."
git tag -a "$TAG_NAME" -m "Release $SEM_VER"
print_success "Git tag created: $TAG_NAME"

# Push tag to origin
print_info "Pushing tag to origin..."
git push origin "$TAG_NAME"
print_success "Tag pushed to origin"

# Show release notes
print_info "Release notes:"
echo "## Release Information"
echo ""
echo "**Version:** $SEM_VER"
echo ""
echo "**Version Details:**"
echo "- Major: $MAJOR"
echo "- Minor: $MINOR"
echo "- Patch: $PATCH"
echo ""
if [ -n "$RELEASE_NOTES" ]; then
    echo "## Release Notes"
    echo ""
    echo "$RELEASE_NOTES"
else
    echo "This release was created automatically from the repository's current version."
    echo "The Docker Release workflow will be automatically triggered to build and push container images."
fi

print_header "Release Created Successfully"
print_success "Tag: $TAG_NAME"
print_success "The GitHub release will be created automatically"
print_success "The Docker Release workflow will build and push images"
echo -e "\n${GREEN}GitHub will process the release automatically. Check your repository's releases page:${NC}"
echo "https://github.com/ITHedgeHog/IntervalGenerator/releases/tag/$TAG_NAME"
