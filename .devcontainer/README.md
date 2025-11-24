# Mockery DevContainer Configuration

This devcontainer provides a fully configured development environment for the Mockery project in GitHub Codespaces or local VS Code.

## Prebuilt Container Image

This devcontainer is automatically prebuilt by GitHub Actions for faster Codespaces startup:
- **Workflow**: `.github/workflows/codespaces-prebuild.yml`
- **Image**: `ghcr.io/busadave13/mockery/devcontainer:latest`
- **Triggers**: On push to main (when devcontainer changes), weekly schedule, manual dispatch

The prebuild includes:
- .NET 9.0 SDK with restored NuGet packages
- Docker and docker-compose
- GitHub CLI (gh)
- All necessary development tools

**Result**: Codespaces start in ~30-60 seconds instead of 3-5 minutes!

## What's Included

### Base Image
- **.NET 9.0 SDK** - Full .NET development environment

### Features
- **Docker-in-Docker** - Run Docker and docker-compose commands inside the container
- **GitHub CLI (gh)** - Manage GitHub issues, PRs, and repositories

### VS Code Extensions
- **C# Dev Kit** - Full C# language support and debugging
- **Docker** - Docker file support and container management
- **EditorConfig** - Consistent coding styles
- **Test Adapter** - Run and debug tests

### Port Forwarding
- **8080** - Mockery API endpoint
- **18888** - Aspire Dashboard UI (auto-opens in browser)
- **18889** - OpenTelemetry OTLP gRPC endpoint
- **18890** - OpenTelemetry OTLP HTTP endpoint

## Quick Start

### In GitHub Codespaces

1. Go to https://github.com/busadave13/Mockery
2. Click the green "Code" button
3. Select "Codespaces" tab
4. Click "Create codespace on main"

### Local Development with DevContainer

1. Install Docker Desktop
2. Install VS Code with "Dev Containers" extension
3. Open this repository in VS Code
4. Click "Reopen in Container" when prompted

## Running the Project

```bash
# Navigate to the Mockery project
cd src/Mockery

# Run the application (Development mode - uses local mocks)
dotnet run

# Run tests
cd ../Mockery.Test
dotnet test

# Start Aspire Dashboard for telemetry
docker compose up -d

# Build Docker image
docker build -t mockery:latest .
```

## Environment

The devcontainer automatically:
- Restores NuGet packages on creation (`dotnet restore`)
- Configures Docker-in-Docker for container operations
- Sets up GitHub CLI for repository management
- Forwards necessary ports for development

## Customization

Edit `.devcontainer/devcontainer.json` to:
- Add more VS Code extensions
- Change port forwarding settings
- Add additional features or tools
- Modify post-create commands
