# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Mockery is a REST API service for serving HTTP mock responses. It provides a single GET endpoint that retrieves mock files from either a local file system (development) or a Git repository (production). This enables teams to:

- **Development**: Quickly test locally with immediate mock file changes (no Git setup required)
- **Production**: Manage mocks through standard Git workflows (commits, pull requests, version control)

The service is built with ASP.NET Core 9.0+ and uses LibGit2Sharp for Git operations in production mode. OpenTelemetry observability is built-in using native OpenTelemetry libraries. No database or authentication required.

## Build and Development Commands

### Building the Solution
```bash
dotnet restore
dotnet build
```

### Running Tests
```bash
# Run all tests
dotnet test src/Mockery.Test/Mockery.Test.csproj

# Run tests from solution root
cd src
dotnet test
```

### Running Locally (Development Mode)

**Local development uses the local file system** - no Git setup required!

**IMPORTANT**: Always run from the `src/Mockery` directory.

```bash
# Navigate to src/Mockery directory
cd src/Mockery

# Run in Development mode (uses Local repository, telemetry enabled by default)
dotnet run

# Application listens on http://localhost:8080
# Mocks are loaded from mocks/ directory at project root
```

**Quick Start - Sample Mocks Included:**
The repository includes sample mocks in `mocks/` that work immediately:
- `FooBar/1234` - JSON response with custom headers
- `FooBar/5678` - HTML response
- `Products/hydrate` - Product catalog
- `Products/error` - Error response

**How It Works:**
- Development mode is configured in `appsettings.Development.json`
- Mock files are read directly from `mocks/` directory (at repository root)
- No Git operations performed (clone/pull disabled)
- Changes to mock files are picked up immediately (no restart needed)
- New mocks can be added by creating files in the `mocks/` directory

**With Telemetry (Default):**
To view telemetry data during development:
1. Start Aspire Dashboard: `docker compose up -d`
2. Run application: `dotnet run` (telemetry enabled by default)
3. View dashboard at http://localhost:18888

### Docker Commands

**Production Docker (Git Repository)**

For production deployments (Azure Container Apps, Kubernetes, etc.), configure Git mode in `appsettings.Production.json`:

```bash
# 1. Configure Git settings in appsettings.Production.json
# Edit src/Mockery/appsettings.Production.json:
# {
#   "MockRepository": {
#     "Type": "Git",
#     "Git": {
#       "RepositoryUrl": "https://github.com/your-org/mockery-mocks.git",
#       "Branch": "main",
#       "ClonePath": "/app/mocks",
#       "AccessToken": "your-token-if-private-repo"
#     }
#   }
# }

# 2. Build Docker image (uses Production environment by default)
docker build -t mockery:latest .

# 3. Run container with Production environment
docker run -d --name mockery -p 8080:8080 \
  -v mockery-data:/app/mocks \
  mockery:latest

# 4. Test endpoints
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock
curl -i -H "X-Mock-ID: Products/hydrate" http://localhost:8080/api/mock

# 5. Stop and remove container
docker stop mockery && docker rm mockery
```

### Testing Mock Endpoints
```bash
# Single mock ID (default 200 OK)
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock

# Multiple mock IDs (random selection)
curl -i -H "X-Mock-ID: FooBar/1234,FooBar/5678,Products/9012" http://localhost:8080/api/mock

# With custom status code
curl -i -H "X-Mock-ID: Products/error" -H "X-Mock-StatusCode: 500" http://localhost:8080/api/mock
```

## Repository Modes

Mockery supports two repository modes, selected by environment:

### Local Mode (Development Environment)

**Used by:**
- Local development: `dotnet run` from `src/Mockery` directory

**Configuration:** Set in `appsettings.Development.json`:
```json
{
  "MockRepository": {
    "Type": "Local",
    "LocalPath": "../.."
  }
}
```

**Characteristics:**
- Uses `LocalFileMockRepository` implementation
- Reads mocks directly from local file system (`mocks/` directory)
- No Git operations (no clone, pull, or LibGit2Sharp dependencies)
- Changes to mock files are immediately available
- Ideal for local development and testing

**Setup:**
1. Create mock files in `mocks/{ServiceName}/{FileId}.{extension}` (at repository root)
2. Navigate to `cd src/Mockery`
3. Run `dotnet run` (listens on http://localhost:8080, telemetry enabled by default)
4. Mock files can be edited while the service is running (no restart needed)

### Git Mode (Production Environment)

**Used by:**
- Production deployments (Azure Container Apps, Kubernetes, etc.)
- Staging environments
- Any Docker container with `ASPNETCORE_ENVIRONMENT=Production`

**Configuration:** Set in `appsettings.Production.json`:
```json
{
  "MockRepository": {
    "Type": "Git",
    "Git": {
      "RepositoryUrl": "https://github.com/your-org/mockery-mocks.git",
      "Branch": "main",
      "ClonePath": "/app/mocks",
      "AccessToken": "",
      "AutoRefresh": {
        "Enabled": true,
        "IntervalMinutes": 5
      }
    }
  }
}
```

**IMPORTANT - Secure Token Configuration:**

**Never store access tokens in appsettings files!** The `AccessToken` field should remain empty in configuration files.

For **public repositories**: Leave `AccessToken` empty (no authentication needed).

For **private repositories**: Set the access token via environment variable:
```bash
MockRepository__Git__AccessToken="ghp_your_github_token_here"
```

ASP.NET Core's configuration system automatically merges environment variables with appsettings, with environment variables taking precedence. Use the double-underscore (`__`) convention to override nested configuration values.

**Characteristics:**
- Uses `GitMockRepository` implementation
- Clones Git repository on startup
- Pulls latest changes when repository already exists
- **Auto-refresh**: Periodically pulls latest changes from Git (configurable interval, default: 5 minutes)
- Mocks managed through Git workflows (commits, PRs, version control)
- Ideal for production, staging, and shared environments
- Configuration is done through appsettings files

**Auto-Refresh Feature:**
- Background service automatically pulls latest changes from Git repository
- Default interval: 5 minutes (configurable via `AutoRefresh.IntervalMinutes`)
- Can be disabled by setting `AutoRefresh.Enabled` to `false`
- Only runs in Git mode (not in Local mode)
- Graceful error handling - continues running even if a refresh fails
- New mocks become available without restarting the service

### Secure Git Access Token Configuration

**Environment Variable:** `MockRepository__Git__AccessToken`

This section explains how to securely configure Git access tokens for private repositories in different deployment environments.

#### Local Development (Private Repos)

For testing with private repositories locally:

**PowerShell (Windows):**
```powershell
$env:MockRepository__Git__AccessToken="ghp_your_token_here"
cd src/Mockery
dotnet run
```

**Bash (Linux/Mac):**
```bash
export MockRepository__Git__AccessToken="ghp_your_token_here"
cd src/Mockery
dotnet run
```

#### Docker Deployment

**Pass as environment variable:**
```bash
docker run -d \
  -e MockRepository__Git__AccessToken="ghp_your_token_here" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -p 8080:8080 \
  mockery:latest
```

**Using Docker Compose:**
```yaml
services:
  mockery:
    image: mockery:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - MockRepository__Git__AccessToken=${GIT_ACCESS_TOKEN}
    ports:
      - "8080:8080"
```

Then run with:
```bash
GIT_ACCESS_TOKEN="ghp_your_token_here" docker-compose up
```

#### Kubernetes/Helm Deployment

**Step 1: Create Kubernetes Secret**
```bash
kubectl create secret generic mockery-git-token \
  --from-literal=access-token="ghp_your_token_here" \
  --namespace=your-namespace
```

**Step 2: Install Helm chart with secret reference**
```bash
helm install mockery oci://ghcr.io/busadave13/helm/mockery \
  --set secrets.gitAccessToken="ghp_your_token_here" \
  --namespace=your-namespace
```

The Helm chart automatically creates the Kubernetes Secret and configures the deployment to use it.

**For production**, use external secret management:
- **Azure**: Azure Key Vault with CSI driver
- **AWS**: AWS Secrets Manager with External Secrets Operator
- **GCP**: Google Secret Manager
- **HashiCorp Vault**: Vault Agent Injector

#### GitHub Personal Access Token (PAT)

To create a GitHub Personal Access Token:

1. Go to GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Click "Generate new token (classic)"
3. Give it a descriptive name (e.g., "Mockery Production")
4. Select scopes:
   - For **public repositories**: No scopes needed
   - For **private repositories**: Select `repo` scope (Full control of private repositories)
5. Click "Generate token"
6. Copy the token immediately (starts with `ghp_`)

**Token Security Best Practices:**
- Never commit tokens to source control
- Use different tokens for different environments
- Rotate tokens regularly
- Use fine-grained personal access tokens when possible
- Set token expiration dates
- Use minimum required scopes

**Environment Variable Overrides:**
- Docker Desktop uses `MockRepository__LocalPath=/app` environment variable to override the path for containerized environments
- All configuration values can be overridden using environment variables with the `__` (double underscore) convention

## Architecture

### High-Level Architecture

Mockery follows a three-layer architecture pattern:

**Presentation Layer**: ASP.NET Core REST API with single GET endpoint, rate limiting middleware, health check endpoints

**Business Logic Layer**: Random selection for multiple mock IDs, status code semantics, mock ID parsing (ServiceName/FileId format)

**Repository Layer**: Git repository operations via LibGit2Sharp, direct file path lookup

### Mock File Organization

**Git Repository Structure:**
```
mocks/
├── FooBar/
│   ├── 1234.json
│   ├── 1234.headers.json        # Optional custom headers
│   └── 5678.html
├── Products/
│   ├── hydrate.json
│   ├── hydrate.headers.json     # Optional custom headers
│   └── error.json
```

**Mock ID Format:**
- Format: `{ServiceName}/{FileId}` (e.g., `FooBar/1234`, `Products/hydrate`)
- Service name must match folder name exactly (case-sensitive)
- Extension determines Content-Type header

### Core Components

**Controllers** (`src/Mockery/Controllers/`):
- `MockController`: Single GET endpoint `/api/mock` for retrieving mock content by mock ID
  - Parses `X-Mock-ID` header (single or comma-separated)
  - Parses optional `X-Mock-StatusCode` header
  - Delegates business logic to `IMockService`

**Business Logic** (`src/Mockery/BusinessLogic/`):
- `IMockService` interface and `MockService` implementation
- Parse mock ID to extract service name and file ID (e.g., `FooBar/1234` → service: `FooBar`, fileId: `1234`)
- Random selection when multiple mock IDs provided
- Apply status code semantics (204/404 return no content, others return mock content)
- Coordinate between repository layer and content-type resolution

**Repository Layer** (`src/Mockery/Repository/`):
- `IGitMockRepository` interface with two implementations:
  - `GitMockRepository`: Git-based operations (clone, pull/refresh via LibGit2Sharp)
  - `LocalFileMockRepository`: Local file system access (no Git operations)
- `FileSystemMockRepositoryBase`: Abstract base class containing shared file lookup logic
- Direct file lookup using path: `mocks/{ServiceName}/{FileId}.*`
- Optional headers file lookup: `mocks/{ServiceName}/{FileId}.headers.json`
- Implementation selected at startup based on `MockRepository.Type` configuration

**Supporting Services** (`src/Mockery/Services/`):
- `ContentTypeResolver`: Maps file extensions to MIME types (.json → application/json, .html → text/html)
- `RandomMockSelector`: Selects random mock from multiple IDs

**Middleware** (`src/Mockery/Middleware/`):
- `RateLimitingMiddleware`: Dual-strategy rate limiting (per-IP and global throttling)
  - Per-IP: Default 100 requests per IP per minute
  - Global: Default 1000 total requests per minute
  - Returns HTTP 429 when limits exceeded

**Authentication**:
- No authentication or authorization required
- Service intended for development/testing environments
- Network-level security recommended (VPN, private networks)

### Configuration

**Environment-Based Configuration:**

Mockery uses ASP.NET Core's environment-based configuration system:

**Development Environment** (`appsettings.Development.json`):
- Used by: Local `dotnet run` and Docker Desktop (`docker-compose up`)
- Repository Type: `Local` (no Git operations)
- LocalPath: `../..` (relative to `src/Mockery`, points to repository root)
- Docker override: `MockRepository__LocalPath=/app` environment variable in docker-compose.yml

```json
{
  "MockRepository": {
    "Type": "Local",
    "LocalPath": "../.."
  }
}
```

**Production Environment** (`appsettings.Production.json`):
- Used by: Azure Container Apps, Kubernetes, production Docker containers
- Repository Type: `Git` (clones from Git repository)
- Requires Git configuration

```json
{
  "MockRepository": {
    "Type": "Git",
    "Git": {
      "RepositoryUrl": "https://github.com/your-org/mockery-mocks.git",
      "Branch": "main",
      "ClonePath": "/app/mocks",
      "AccessToken": ""
    }
  }
}
```

**Important Notes:**
- Environment is controlled by `ASPNETCORE_ENVIRONMENT` variable
- Local development defaults to `Development` environment
- Production deployments should set `ASPNETCORE_ENVIRONMENT=Production`
- Environment variables can override appsettings using `__` syntax (e.g., `MockRepository__LocalPath=/app`)
- Launch profiles are configured in `src/Mockery/Properties/launchSettings.json`

**Rate Limiting Configuration (appsettings.json):**
```json
{
  "RateLimiting": {
    "Enabled": true,
    "PerIp": {
      "Enabled": true,
      "PermitLimit": 100,
      "Window": "00:01:00"
    },
    "Global": {
      "Enabled": true,
      "PermitLimit": 1000,
      "Window": "00:01:00"
    }
  }
}
```

### Observability

**OpenTelemetry Integration:**

Mockery includes built-in OpenTelemetry observability using native OpenTelemetry libraries and standard OTEL environment variables:

```csharp
// In Program.cs
using Mockery.Extensions;

// Add OpenTelemetry observability (configured via environment variables)
builder.AddObservability();

// Add OpenTelemetry observability middleware
app.UseObservability();
```

**Features:**
- **Logs**: Structured logging with OpenTelemetry exporters
- **Traces**: Distributed tracing with automatic HTTP instrumentation
- **Metrics**: Request counts, durations, and custom application metrics (exposed at `/metrics` endpoint)

**Configuration (Standard OTEL Environment Variables):**

Observability is configured using standard OpenTelemetry environment variables:

| Environment Variable | Description | Example |
|---------------------|-------------|---------|
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP endpoint URL | `http://aspire-dashboard:18889` |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | Protocol (grpc or http/protobuf) | `grpc` (default) |
| `OTEL_SERVICE_NAME` | Service name for telemetry | `mockery` |

**Development Environment (Aspire Dashboard):**
- Start Aspire Dashboard: `docker compose up -d`
- Run with telemetry: `dotnet run` (telemetry enabled by default)
- Environment variables set in launch profile (launchSettings.json):
  ```yaml
  - OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:18889
  - OTEL_EXPORTER_OTLP_PROTOCOL=grpc
  - OTEL_SERVICE_NAME=Mockery
  ```
- Access dashboard at http://localhost:18888
- Visualize logs, traces, and metrics in real-time

**Production Environment (Kubernetes):**
- Telemetry exported to Aspire via OTLP endpoint configured in Helm chart
- Environment variables set in deployment (via values.yaml):
  ```yaml
  - OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire.monitor.svc.cluster.local:18889
  - OTEL_EXPORTER_OTLP_PROTOCOL=grpc
  - OTEL_SERVICE_NAME=Mockery
  ```
- Metrics endpoint `/metrics` available for Prometheus scraping

**Implementation Details:**
- Implementation: `src/Mockery/Extensions/OpenTelemetryExtensions.cs`
- Uses standard OpenTelemetry environment variables for configuration
- Automatic fallback to console exporters if OTLP endpoint not configured
- Includes ASP.NET Core and HTTP client instrumentation
- Supports Prometheus metrics endpoint at `/metrics`

### Mock Retrieval Flow

1. Client sends GET request to `/api/mock` with headers:
   ```http
   X-Mock-ID: FooBar/1234
   X-Mock-StatusCode: 500  (optional)
   ```
   Or multiple IDs for random selection:
   ```http
   X-Mock-ID: FooBar/1234,FooBar/5678,Products/9012
   ```

2. Controller parses headers and validates format

3. Business logic:
   - Parses mock ID to extract service name and file ID
   - If multiple IDs, randomly selects one
   - Applies status code semantics (204/404 = no content, others = return content)
   - Retrieves mock file via repository layer

4. Repository layer uses direct path lookup: `mocks/{ServiceName}/{FileId}.*`

5. Controller sets HTTP status code and headers, returns response

### Status Code Behavior

| Status Code | Mock Content Returned? | Custom Headers Returned? |
|-------------|----------------------|-------------------------|
| **204** (No Content) | ❌ No | ✅ Yes |
| **404** (Not Found) | ❌ No | ✅ Yes (if .headers.json exists) |
| **2xx/3xx/4xx/5xx** (Other) | ✅ Yes | ✅ Yes |
| **Default** (no header) | ✅ Yes (200 OK) | ✅ Yes |

### Testing

Tests use xUnit, Moq, and FluentAssertions. Test structure:
```
src/Mockery.Test/
├── Controllers/
│   └── MockControllerTests.cs
├── Services/
│   ├── MockServiceTests.cs
│   ├── ContentTypeResolverTests.cs
│   └── RandomMockSelectorTests.cs
└── Repository/
    ├── GitMockRepositoryTests.cs
    └── LocalFileMockRepositoryTests.cs
```

**Unit Testing:**
- Use Moq to mock `IGitMockRepository`
- Test status code semantics and random selection
- Repository tests use temporary directories for file system operations

**Integration Testing:** Use `WebApplicationFactory<Program>` for end-to-end testing

### Health Check Endpoints

- **GET /health/live**: Liveness probe (application is running)
- **GET /health/ready**: Readiness probe (Git repository accessible)
- **GET /health/startup**: Startup probe (initial Git clone completed)

Used by Kubernetes/container orchestrators for health monitoring.

## Deployment

### GitHub Container Registry (GHCR)

**GitHub Actions Workflow:** `.github/workflows/publish-docker-helm.yml`

**Triggers:**
- Pull request merges to main branch
- Manual workflow dispatch

**Jobs:**
1. **Tests**: Runs all 44 unit tests
2. **Builds**: Compiles the application and Docker image
3. **Publishes**: Pushes Docker image and Helm chart to GitHub Container Registry

**Features:**
- No authentication setup required (uses GITHUB_TOKEN)
- Automatic versioning with GitVersion
- OCI-based Helm chart publishing
- Docker images published to `ghcr.io/busadave13/mockery`
- Helm charts published to `oci://ghcr.io/busadave13/helm/mockery`

**Accessing Published Artifacts:**
```bash
# Pull Docker image
docker pull ghcr.io/busadave13/mockery:latest

# Install Helm chart
helm install mockery oci://ghcr.io/busadave13/helm/mockery --version 1.0.0
```

## Common Development Patterns

### Adding a New Mock (Local Development)

For local development (default when running `dotnet run`):

1. Navigate to project root (repository root, not src/Mockery)
2. Create service folder if needed: `mkdir -p mocks/MyService`
3. Create mock file: `echo '{"status":"success"}' > mocks/MyService/1234.json`
4. Optionally create headers file: `echo '{"X-Custom-Header":"value"}' > mocks/MyService/1234.headers.json`
5. Test immediately - no restart needed!

```bash
curl -i -H "X-Mock-ID: MyService/1234" http://localhost:8080/api/mock
```

### Adding a New Mock (Production/Git Mode)

For production or when using Git mode:

1. Clone mock repository locally
2. Create service folder if needed: `mkdir mocks/MyService`
3. Create mock file: `echo '{"status":"success"}' > mocks/MyService/1234.json`
4. Optionally create headers file: `echo '{"X-Custom-Header":"value"}' > mocks/MyService/1234.headers.json`
5. Commit and push: `git add mocks/MyService/ && git commit -m "Add mock" && git push`
6. Service automatically pulls latest changes (or restart to force refresh)

### Extending Business Logic

1. Add method to `IMockService` interface
2. Implement in `MockService` class
3. Inject `IGitMockRepository` dependency for file operations
4. Return domain objects (e.g., `MockFileResult`), not HTTP responses
5. Let controller handle HTTP concerns (status codes, headers)

### Adding Repository Methods

1. Add method to `IGitMockRepository` interface
2. Decide if the method should be:
   - In the base class `FileSystemMockRepositoryBase` (shared file operations)
   - Specific to `GitMockRepository` (Git operations only)
   - Specific to `LocalFileMockRepository` (local-only operations)
3. For Git-specific operations: Use LibGit2Sharp in `GitMockRepository`
4. For file operations: Use direct file path lookup in base class: `mocks/{ServiceName}/{FileId}.*`
5. Handle file not found scenarios gracefully
