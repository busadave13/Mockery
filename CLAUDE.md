# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Mockery is a REST API service for serving HTTP mock responses with support for both local file system (development) and Git-based storage (production). It's built with .NET 9.0 and uses ASP.NET Core.

## Development Commands

### Building and Testing

```bash
# Restore and build from repository root
dotnet restore
dotnet build

# Run all tests (44 comprehensive tests)
dotnet test

# Run tests with verbose output
dotnet test --verbosity normal
```

### Running the Application

```bash
# Run locally with dotnet (uses Development mode automatically)
# ALWAYS run from src/Mockery directory
cd src/Mockery
dotnet run --urls "http://localhost:8080"

# Run with Docker Compose (includes Aspire Dashboard)
docker-compose up -d

# Build Docker image
docker build -t mockery:latest .
```

### Testing Mock Endpoints

```bash
# Test with sample mock
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock

# Test health checks
curl http://localhost:8080/health/live
curl http://localhost:8080/health/ready
curl http://localhost:8080/health/startup
```

## Architecture

### Repository Pattern with Strategy

The codebase uses the Strategy pattern to support different storage backends:

```
IGitMockRepository (interface)
       ↑
       |
FileSystemMockRepositoryBase (abstract)
       ↑
       |
   ┌───┴───┐
   |       |
GitMock   LocalFile
Repository  MockRepository
```

**Key Points:**
- `FileSystemMockRepositoryBase` contains shared file lookup logic (`FindMockFileAsync`, `FindHeadersFileAsync`)
- `GitMockRepository` handles Git operations (clone, pull, auto-refresh) using LibGit2Sharp
- `LocalFileMockRepository` provides direct file system access with no Git dependencies
- Repository implementation is selected at startup based on `MockRepository.Type` configuration

### Storage Modes

**Local Mode (Development):**
- Configured in `appsettings.Development.json` with `"Type": "Local"`
- Direct file system access from `mocks/` directory at repository root
- No Git operations or dependencies
- Changes picked up immediately (no restart needed)

**Git Mode (Production/Docker):**
- Configured in `appsettings.Production.json` with `"Type": "Git"`
- Clones Git repository on startup
- Periodic auto-refresh via `GitRepositoryRefreshService`
- Required for Docker/Kubernetes deployments

### Configuration Files

- `appsettings.json` - Base configuration (Local mode default)
- `appsettings.Development.json` - Local development (file system mode)
- `appsettings.Production.json` - Production/Docker (Git mode)
- All configuration is in appsettings files, no environment variables needed for repository config

### Mock File Structure

Mock files are located at repository root in the `mocks/` directory:

```
mocks/
├── {ServiceName}/
│   ├── {FileId}.json              # Response body
│   ├── {FileId}.headers.json      # Optional custom headers
│   └── {StatusCode}.status.json   # Optional status code responses
```

**Mock ID Format:** `{Path}/{FileId}`
- Path: Directory path to mock file (can include subfolders like `FooBar/staging`)
- FileId: Filename without extension (always the last segment after `/`)
- Examples: `FooBar/1234`, `Products/staging/test`, `test/prod/success`

**File Types:**
- `.json` → `application/json`
- `.html` → `text/html`
- `.xml` → `application/xml`
- `.txt` → `text/plain`
- `.css` → `text/css`
- `.js` → `application/javascript`

**Status Files:**
- Named `{statusCode}.status.json` (e.g., `404.status.json`, `500.status.json`)
- Status code is extracted from the filename
- Can be empty (status only) or contain JSON response body

### OpenTelemetry Configuration

**IMPORTANT:** Telemetry configuration is handled EXCLUSIVELY via environment variables, NOT appsettings files.

**Standard OTEL Environment Variables:**
- `OTEL_SERVICE_NAME` - Service identifier (default: "Mockery")
- `OTEL_EXPORTER_OTLP_ENDPOINT` - Telemetry endpoint URL (e.g., `http://localhost:18889`)
- `OTEL_EXPORTER_OTLP_PROTOCOL` - Export protocol (default: "grpc")

**Launch Profile:**
- The `Mockery` launch profile in `launchSettings.json` includes telemetry environment variables
- Running `dotnet run` from `src/Mockery` enables telemetry automatically

**Aspire Dashboard:**
- Start with: `docker-compose up -d`
- Access at: http://localhost:18888
- OTLP endpoint: http://localhost:18889

## Key Classes and Files

### Controllers
- `MockController.cs` - Main API endpoint (`GET /api/mock`)

### Business Logic
- `MockService.cs` - Mock file retrieval and processing
- `IMockService.cs` - Service interface

### Repository Layer
- `IGitMockRepository.cs` - Repository interface
- `FileSystemMockRepositoryBase.cs` - Shared file lookup logic (abstract base)
- `GitMockRepository.cs` - Git-based implementation (LibGit2Sharp)
- `LocalFileMockRepository.cs` - Local file system implementation

### Services
- `ContentTypeResolver.cs` - Maps file extensions to content types
- `GitRepositoryRefreshService.cs` - Background service for periodic Git refresh (Git mode only)

### Configuration
- `MockRepositorySettings.cs` - Repository configuration model
- `GitRepositoryOptions.cs` - Git-specific options

### Extensions
- `OpenTelemetryExtensions.cs` - OpenTelemetry setup (traces, metrics, logs)

## Project Structure

```
Mockery/
├── mocks/                          # Sample mocks (repository root)
├── src/
│   ├── Mockery/                    # Main application
│   │   ├── Controllers/
│   │   ├── BusinessLogic/
│   │   ├── Repository/
│   │   ├── Services/
│   │   ├── Configuration/
│   │   ├── Extensions/
│   │   ├── Models/
│   │   ├── Properties/
│   │   │   └── launchSettings.json
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── appsettings.Production.json
│   │   └── Program.cs
│   └── Mockery.Test/               # Unit tests (xUnit, Moq, FluentAssertions)
├── charts/mockery/                 # Helm chart
├── Dockerfile                      # Production Docker image
├── docker-compose.yml              # Local development with Aspire Dashboard
└── .github/workflows/
    └── publish-docker-helm.yml     # CI/CD pipeline
```

## Testing

**Test Structure:**
- Framework: xUnit
- Mocking: Moq
- Assertions: FluentAssertions
- 44 comprehensive tests covering:
  - Controllers (MockController)
  - Services (ContentTypeResolver, MockService)
  - Repository (GitMockRepository, LocalFileMockRepository)
  - All edge cases and error scenarios

**Running Tests:**
```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal
```

## CI/CD Pipeline

**Workflow:** `.github/workflows/publish-docker-helm.yml`

**Triggers:**
- PR merge to main branch
- Manual workflow dispatch

**Steps:**
1. Run tests (`dotnet test`)
2. Build Docker image
3. Push to GitHub Container Registry (`ghcr.io/busadave13/mockery`)
4. Package and push Helm chart to OCI registry
5. Create GitHub release (on PR merge only)

**Versioning:**
- Uses GitVersion with `.gitversion.yml`
- Semantic versioning (major.minor.patch)
- Automatic version bumping

## Helm Deployment

**Install from OCI registry:**
```bash
helm install mockery oci://ghcr.io/busadave13/helm/mockery --version <version>
```

**Chart location:** `charts/mockery/`

**Key configuration:**
- `config.aspnetcoreEnvironment` - Always "Production" for Kubernetes
- `persistence.enabled` - Required for Git mode
- `config.otlpEndpoint` - OpenTelemetry collector endpoint

## Dependencies

**Core:**
- .NET 9.0
- LibGit2Sharp (Git operations)
- OpenTelemetry packages (observability)

**Testing:**
- xUnit
- Moq
- FluentAssertions
- Microsoft.AspNetCore.Mvc.Testing

**All dependencies are public NuGet packages - no private repositories or authentication required.**

## Common Patterns

### Dependency Injection
Repository implementation is selected at startup in `Program.cs`:
```csharp
if (mockRepoSettings.Type.Equals("Local", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IGitMockRepository, LocalFileMockRepository>();
}
else
{
    builder.Services.AddSingleton<IGitMockRepository, GitMockRepository>();
}
```

### Health Checks
- `/health/live` - Liveness probe (always healthy if running)
- `/health/ready` - Readiness probe (checks if mocks directory exists)
- `/health/startup` - Startup probe (checks initialization complete)

### Error Handling
- Missing mock files return HTTP 404
- Invalid Mock-ID header returns HTTP 400
- Status files control HTTP status codes (e.g., `500.status.json` returns 500)

## Important Notes

- **Always run `dotnet run` from the `src/Mockery` directory**
- Mock files are at repository root: `mocks/{ServiceName}/{FileId}.{extension}`
- Local mode changes are picked up immediately (no restart needed)
- Git mode requires restart to pull latest changes (or wait for auto-refresh)
- OpenTelemetry config is via environment variables, NOT appsettings files
- The `.clinerules` file contains legacy Cline rules - these are superseded by this CLAUDE.md file
