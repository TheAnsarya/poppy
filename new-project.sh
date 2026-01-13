#!/bin/bash
# Script to create a new retro game project from the project-base template

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_BASE="${SCRIPT_DIR}/project-base"

# Function to print colored messages
print_error() {
    echo -e "${RED}Error: $1${NC}" >&2
}

print_success() {
    echo -e "${GREEN}$1${NC}"
}

print_info() {
    echo -e "${YELLOW}$1${NC}"
}

# Function to show usage
usage() {
    cat << EOF
Usage: $0 <project-name> [destination-directory]

Create a new retro game project from the project-base template.

Arguments:
    project-name           Name of your new project
    destination-directory  Optional: Where to create the project (default: current directory)

Example:
    $0 my-awesome-game
    $0 my-awesome-game ~/projects/

EOF
    exit 1
}

# Check arguments
if [ $# -lt 1 ]; then
    print_error "Project name is required"
    usage
fi

PROJECT_NAME="$1"
DEST_DIR="${2:-.}"

# Validate project name
if [[ ! "$PROJECT_NAME" =~ ^[a-zA-Z0-9_-]+$ ]]; then
    print_error "Project name can only contain letters, numbers, hyphens, and underscores"
    exit 1
fi

# Check if project-base exists
if [ ! -d "$PROJECT_BASE" ]; then
    print_error "project-base directory not found at: $PROJECT_BASE"
    exit 1
fi

# Create destination directory if it doesn't exist
mkdir -p "$DEST_DIR"

# Full path for the new project
PROJECT_PATH="${DEST_DIR}/${PROJECT_NAME}"

# Check if project already exists
if [ -d "$PROJECT_PATH" ]; then
    print_error "Project directory already exists: $PROJECT_PATH"
    exit 1
fi

print_info "Creating new project: $PROJECT_NAME"

# Copy project-base to new location
cp -r "$PROJECT_BASE" "$PROJECT_PATH"

print_success "✓ Project files copied to: $PROJECT_PATH"

# Update project name in Makefile if it exists
if [ -f "$PROJECT_PATH/Makefile" ]; then
    sed -i "s/PROJECT = game/PROJECT = $PROJECT_NAME/" "$PROJECT_PATH/Makefile" 2>/dev/null || \
    sed -i '' "s/PROJECT = game/PROJECT = $PROJECT_NAME/" "$PROJECT_PATH/Makefile" 2>/dev/null || true
    print_success "✓ Updated Makefile with project name"
fi

print_success "\n🎮 Project created successfully!"
print_info "\nNext steps:"
print_info "  cd $PROJECT_PATH"
print_info "  # Edit files in src/ to create your game"
print_info "  # Add assets to assets/ directories"
print_info "  # Build with: make"
