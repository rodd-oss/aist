#!/bin/bash

set -e

REPO="rodd-oss/aist"
INSTALL_DIR="/usr/local/bin"
CLI_NAME="aist"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Print functions
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Detect OS and architecture
detect_platform() {
    OS=$(uname -s | tr '[:upper:]' '[:lower:]')
    ARCH=$(uname -m)

    case "$OS" in
        linux)
            PLATFORM="linux"
            ;;
        darwin)
            PLATFORM="osx"
            ;;
        *)
            print_error "Unsupported operating system: $OS"
            exit 1
            ;;
    esac

    case "$ARCH" in
        x86_64|amd64)
            ARCH="x64"
            ;;
        arm64|aarch64)
            ARCH="arm64"
            ;;
        *)
            print_error "Unsupported architecture: $ARCH"
            exit 1
            ;;
    esac

    RUNTIME="${PLATFORM}-${ARCH}"
    print_info "Detected platform: $RUNTIME"
}

# Get latest release version
get_latest_version() {
    print_info "Fetching latest release..."

    LATEST_URL=$(curl -s "https://api.github.com/repos/$REPO/releases/latest" | grep '"tag_name":' | head -n 1 | sed -E 's/.*"([^"]+)".*/\1/')

    if [ -z "$LATEST_URL" ]; then
        print_error "Failed to fetch latest release"
        exit 1
    fi

    VERSION="$LATEST_URL"
    print_info "Latest version: $VERSION"
}

# Download and install
download_and_install() {
    local version=$1
    local runtime=$2

    # Remove 'v' prefix if present for URL
    local version_no_v="${version#v}"

    FILENAME="${CLI_NAME}-${runtime}.tar.gz"
    DOWNLOAD_URL="https://github.com/${REPO}/releases/download/${version}/${FILENAME}"
    TEMP_DIR=$(mktemp -d)

    print_info "Downloading ${CLI_NAME} ${version} for ${runtime}..."

    if ! curl -sL "$DOWNLOAD_URL" -o "${TEMP_DIR}/${FILENAME}"; then
        print_error "Failed to download from: $DOWNLOAD_URL"
        rm -rf "$TEMP_DIR"
        exit 1
    fi

    print_info "Extracting..."
    cd "$TEMP_DIR"
    tar -xzf "$FILENAME"

    if [ ! -f "$CLI_NAME" ]; then
        print_error "Binary not found in archive"
        rm -rf "$TEMP_DIR"
        exit 1
    fi

    chmod +x "$CLI_NAME"

    # Check if we need sudo
    if [ -w "$INSTALL_DIR" ]; then
        mv "$CLI_NAME" "$INSTALL_DIR/"
    else
        print_info "Installing to $INSTALL_DIR (requires sudo)..."
        sudo mv "$CLI_NAME" "$INSTALL_DIR/"
    fi

    rm -rf "$TEMP_DIR"

    print_info "${CLI_NAME} ${version} installed successfully!"
}

# Verify installation
verify_installation() {
    if command -v "$CLI_NAME" &> /dev/null; then
        print_info "Installation verified:"
        "$CLI_NAME" --version 2>/dev/null || print_warning "Could not get version"
    else
        print_warning "${CLI_NAME} is not in PATH. You may need to add $INSTALL_DIR to your PATH."
    fi
}

# Main
main() {
    print_info "Installing ${CLI_NAME} CLI..."

    # Allow custom version via argument
    if [ $# -ge 1 ]; then
        VERSION="$1"
        print_info "Using specified version: $VERSION"
    else
        get_latest_version
    fi

    detect_platform
    download_and_install "$VERSION" "$RUNTIME"
    verify_installation

    print_info "Installation complete! Run '${CLI_NAME} --help' to get started."
}

main "$@"
