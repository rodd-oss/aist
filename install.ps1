#Requires -Version 5.1

<#
.SYNOPSIS
    Install script for Aist CLI on Windows
.DESCRIPTION
    Downloads and installs the latest (or specified) version of Aist CLI
.PARAMETER Version
    Specific version to install (e.g., "v1.0.0"). If not specified, installs the latest version.
.PARAMETER InstallDir
    Directory to install the binary. Defaults to a location in PATH.
.EXAMPLE
    .\install.ps1
.EXAMPLE
    .\install.ps1 -Version v1.0.0
.EXAMPLE
    .\install.ps1 -InstallDir "C:\Tools"
#>

param(
    [string]$Version,
    [string]$InstallDir = $null
)

$ErrorActionPreference = "Stop"

# Configuration
$Repo = "rodd-oss/aist"
$CliName = "aist"
$CliExe = "${CliName}.exe"

# Colors for output
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Cyan = "Cyan"

function Write-Info($Message) {
    Write-Host "[INFO] $Message" -ForegroundColor $Green
}

function Write-Error($Message) {
    Write-Host "[ERROR] $Message" -ForegroundColor $Red
}

function Write-Warning($Message) {
    Write-Host "[WARNING] $Message" -ForegroundColor $Yellow
}

function Write-Step($Message) {
    Write-Host $Message -ForegroundColor $Cyan
}

# Detect platform
function Get-Platform {
    $arch = $env:PROCESSOR_ARCHITECTURE

    switch ($arch) {
        "AMD64" { $arch = "x64" }
        "ARM64" { $arch = "arm64" }
        default {
            Write-Error "Unsupported architecture: $arch"
            exit 1
        }
    }

    $runtime = "win-$arch"
    Write-Info "Detected platform: $runtime"
    return $runtime
}

# Get installation directory
function Get-InstallDirectory {
    param([string]$CustomDir)

    if ($CustomDir) {
        $dir = $CustomDir
    } else {
        # Try to find a good location in PATH
        $possibleDirs = @(
            "$env:LOCALAPPDATA\Microsoft\WindowsApps",
            "$env:USERPROFILE\.local\bin",
            "$env:LOCALAPPDATA\Programs"
        )

        $dir = $possibleDirs | Where-Object {
            Test-Path $_ -ErrorAction SilentlyContinue
        } | Select-Object -First 1

        if (-not $dir) {
            $dir = "$env:USERPROFILE\.local\bin"
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
        }
    }

    # Create directory if it doesn't exist
    if (-not (Test-Path $dir)) {
        Write-Info "Creating directory: $dir"
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }

    return $dir
}

# Get latest version from GitHub
function Get-LatestVersion {
    Write-Info "Fetching latest release..."

    try {
        $response = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repo/releases/latest"
        $version = $response.tag_name
        Write-Info "Latest version: $version"
        return $version
    }
    catch {
        Write-Error "Failed to fetch latest release: $_"
        exit 1
    }
}

# Download and install
function Install-Aist {
    param(
        [string]$Version,
        [string]$Runtime,
        [string]$InstallDir
    )

    $filename = "${CliName}-${Runtime}.zip"
    $downloadUrl = "https://github.com/$Repo/releases/download/$Version/$filename"
    $tempDir = [System.IO.Path]::GetTempPath()
    $downloadPath = Join-Path $tempDir $filename
    $extractPath = Join-Path $tempDir "aist-install-$([Guid]::NewGuid().ToString().Substring(0,8))"

    Write-Info "Downloading $CliName $Version for $Runtime..."
    Write-Info "URL: $downloadUrl"

    try {
        Invoke-WebRequest -Uri $downloadUrl -OutFile $downloadPath -UseBasicParsing
    }
    catch {
        Write-Error "Failed to download: $_"
        exit 1
    }

    Write-Info "Extracting..."
    Expand-Archive -Path $downloadPath -DestinationPath $extractPath -Force

    $binaryPath = Join-Path $extractPath $CliExe
    if (-not (Test-Path $binaryPath)) {
        # Try without .exe extension (in case archive has different structure)
        $binaryPath = Join-Path $extractPath $CliName
    }

    if (-not (Test-Path $binaryPath)) {
        Write-Error "Binary not found in archive"
        exit 1
    }

    $targetPath = Join-Path $InstallDir $CliExe

    Write-Info "Installing to $targetPath..."

    # Remove existing installation if present
    if (Test-Path $targetPath) {
        Write-Warning "Removing existing installation..."
        Remove-Item $targetPath -Force
    }

    Move-Item $binaryPath $targetPath -Force

    # Cleanup
    Remove-Item $downloadPath -Force
    Remove-Item $extractPath -Recurse -Force

    Write-Info "$CliName $Version installed successfully!"
}

# Add to PATH if needed
function Add-ToPath {
    param([string]$InstallDir)

    $currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
    $paths = $currentPath -split ";"

    if ($paths -notcontains $InstallDir) {
        Write-Info "Adding $InstallDir to PATH..."
        $newPath = "$currentPath;$InstallDir"
        [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
        Write-Info "Added to user PATH. Restart your terminal to use 'aist' command."
    } else {
        Write-Info "Install directory already in PATH"
    }
}

# Verify installation
function Test-Installation {
    param([string]$InstallDir)

    $binaryPath = Join-Path $InstallDir $CliExe

    if (Test-Path $binaryPath) {
        Write-Info "Installation verified at: $binaryPath"

        try {
            $version = & $binaryPath --version 2>$null
            if ($version) {
                Write-Info "Version: $version"
            }
        }
        catch {
            Write-Warning "Could not verify version"
        }
    } else {
        Write-Error "Installation failed - binary not found"
        exit 1
    }
}

# Main
function Main {
    Write-Step @"

    ╔═══════════════════════════════════════╗
    ║     Aist CLI Installer for Windows    ║
    ╚═══════════════════════════════════════╝

"@

    # Determine version
    if ($Version) {
        Write-Info "Using specified version: $Version"
    } else {
        $Version = Get-LatestVersion
    }

    # Detect platform
    $runtime = Get-Platform

    # Get install directory
    $installDir = Get-InstallDirectory -CustomDir $InstallDir
    Write-Info "Install directory: $installDir"

    # Install
    Install-Aist -Version $Version -Runtime $runtime -InstallDir $installDir

    # Add to PATH
    Add-ToPath -InstallDir $installDir

    # Verify
    Test-Installation -InstallDir $installDir

    Write-Step @"

    ╔═══════════════════════════════════════╗
    ║    Installation Complete!             ║
    ╠═══════════════════════════════════════╣
    ║  Run 'aist --help' to get started     ║
    ╚═══════════════════════════════════════╝

"@

    Write-Info "You may need to restart your terminal for PATH changes to take effect."
}

# Run main
Main
