# Aist

AI-assisted project management for software developers and AI agents.

Aist is a lightweight project management tool designed for the AI era. It bridges the gap between human project managers and AI developer agents through a simple CLI interface and REST API.

## Features

- **Project Management**: Create and manage projects
- **Job Tracking**: Track features, fixes, refactors, chores, formatting, and documentation tasks
- **User Stories**: Convert jobs into actionable user stories with acceptance criteria
- **Progress Logging**: Keep track of development progress
- **AI-Native**: Built for both human developers and AI agents
- **Native Performance**: Distributed as Native AOT binaries for fast startup and minimal dependencies

## Architecture

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────┐
│   Aist CLI      │────▶│  Aist Backend    │────▶│  SQLite DB  │
│  (This repo)    │     │   (REST API)     │     │  (aist/)    │
└─────────────────┘     └──────────────────┘     └─────────────┘
```

- **Aist.Cli**: Command-line interface for interacting with the system
- **Aist.Backend**: ASP.NET Core Web API
- **Database**: SQLite for zero-configuration setup

## Installation

### Quick Install

#### macOS / Linux

```bash
curl -sSL https://raw.githubusercontent.com/rodd-oss/aist/main/install.sh | bash
```

Or with a specific version:
```bash
curl -sSL https://raw.githubusercontent.com/rodd-oss/aist/main/install.sh | bash -s v1.0.0
```

#### Windows

**PowerShell:**
```powershell
irm https://raw.githubusercontent.com/rodd-oss/aist/main/install.ps1 | iex
```

**Or download and run:**
```powershell
# Download the installer
Invoke-WebRequest -Uri https://raw.githubusercontent.com/rodd-oss/aist/main/install.ps1 -OutFile install.ps1

# Run it
.\install.ps1

# Or with a specific version
.\install.ps1 -Version v1.0.0
```

### Manual Installation

Download the appropriate binary for your platform from the [Releases](https://github.com/rodd-oss/aist/releases) page:

| Platform | Architecture | Download |
|----------|-------------|----------|
| Linux | x64 | `aist-linux-x64.tar.gz` |
| Linux | ARM64 | `aist-linux-arm64.tar.gz` |
| macOS | x64 | `aist-osx-x64.tar.gz` |
| macOS | ARM64 (Apple Silicon) | `aist-osx-arm64.tar.gz` |
| Windows | x64 | `aist-win-x64.zip` |
| Windows | ARM64 | `aist-win-arm64.zip` |

Extract and place the binary in a directory in your PATH.

## Usage

### Setup Backend

Before using the CLI, you need to run the backend server:

```bash
# Using Docker
docker-compose up -d

# Or run locally
cd src/Aist.Backend
dotnet run
```

The backend will be available at `http://localhost:5192/api` by default.

### CLI Commands

```bash
# Show help
aist --help

# Project management
aist project list
aist project create --title "My New Project"
aist project delete --id <project-id>

# Job management
aist job list
aist job list --project-id <project-id>
aist job create --project-id <id> --type feature --title "Add login" --description "..." --slug add-login
aist job pull --job-id <id>
aist job done --job-id <id> --pr-title "..." --pr-description "..."

# User stories
aist story list --job-id <id>
aist story create --job-id <id> --title "..." --who "..." --what "..." --why "..." --priority 1
aist story complete --story-id <id>

# Acceptance criteria
aist criteria list --story-id <id>
aist criteria create --story-id <id> --description "..."
aist criteria check --criteria-id <id>
aist criteria uncheck --criteria-id <id>

# Progress logs
aist log list --story-id <id>
aist log add --story-id <id> --text "..."
```

### Environment Variables

```bash
# Backend URL (default: http://localhost:5192/api)
export AIST_API_URL=http://localhost:5192/api

# Database path for backend (relative to backend working directory)
export ConnectionStrings__DefaultConnection="Data Source=aist/main.db"
```

## Development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Building

```bash
# Build entire solution
dotnet build Aist.slnx

# Build CLI only
dotnet build src/Aist.Cli/Aist.Cli.csproj

# Build Backend only
dotnet build src/Aist.Backend/Aist.Backend.csproj
```

### Running

```bash
# Run backend
cd src/Aist.Backend
dotnet run

# Run CLI
cd src/Aist.Cli
dotnet run -- <command>
```

### Publishing Native AOT

```bash
# Publish for current platform
dotnet publish src/Aist.Cli/Aist.Cli.csproj \
  -c Release \
  -p:PublishAot=true \
  -p:PublishSingleFile=true \
  --self-contained \
  -o ./publish

# Publish for specific platform
dotnet publish src/Aist.Cli/Aist.Cli.csproj \
  -c Release \
  -r linux-x64 \
  -p:PublishAot=true \
  -p:PublishSingleFile=true \
  --self-contained \
  -o ./publish/linux-x64
```

### Running Tests

```bash
dotnet test
```

## Docker

```bash
# Build and run with Docker Compose
docker-compose up -d

# View logs
docker-compose logs -f

# Stop
docker-compose down
```

## Workflow

Aist is designed around a simple but powerful workflow:

### 1. Planning Phase
```bash
# Create a project
aist project create --title "Website Redesign"

# Create jobs with detailed descriptions
aist job create \
  --project-id <id> \
  --type feature \
  --title "Implement dark mode" \
  --description "Add dark mode toggle and theme support" \
  --slug dark-mode

# Break down into user stories
aist story create \
  --job-id <id> \
  --title "Theme toggle component" \
  --who "user" \
  --what "toggle between light and dark themes" \
  --why "reduce eye strain in low light" \
  --priority 1

# Add acceptance criteria
aist criteria create \
  --story-id <id> \
  --description "Toggle button visible in header"

aist criteria create \
  --story-id <id> \
  --description "Theme preference persists across sessions"
```

### 2. Development Phase
```bash
# Pull the job (creates git branch)
aist job pull --job-id <id>

# Work through stories, logging progress
aist log add --story-id <id> --text "Created ThemeContext provider"
aist criteria check --criteria-id <id>

# Mark story complete when all criteria met
aist story complete --story-id <id>
```

### 3. Completion Phase
```bash
# Create PR and mark job done
aist job done \
  --job-id <id> \
  --pr-title "feat: implement dark mode" \
  --pr-description "Implements theme toggle..."
```

## API

The backend exposes a REST API. See `src/Aist.Backend/Controllers/` for endpoints.

Default base URL: `http://localhost:5192`

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

[MIT](LICENSE)

## Support

For issues and feature requests, please use the [GitHub Issues](https://github.com/rodd-oss/aist/issues) page.
