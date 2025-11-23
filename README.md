# Mockery

REST API service for serving HTTP mock responses with support for both local file system (development) and Git-based storage (production). Mockery enables:

- **Local Development**: Instant testing with file system-based mocks (no setup required)
- **Production**: Full Git workflow management (commits, pull requests, version control)

## Features

- **Dual Storage Modes**:
  - **Local Mode**: Direct file system access for rapid local development
  - **Git Mode**: Repository-based storage with full version control
- **Single GET Endpoint**: Simple API with header-based mock selection
- **Random Selection**: Support for multiple mock IDs with random selection
- **Custom Headers**: Optional headers files for custom HTTP response headers
- **Status Code Control**: Dynamic status code behavior via request headers
- **Rate Limiting**: Dual-strategy rate limiting (per-IP and global)
- **OpenTelemetry Observability**: Built-in logs, metrics, and traces
- **Aspire Dashboard Integration**: Development environment includes Aspire Dashboard for telemetry visualization
- **No Authentication**: Designed for development/testing environments
- **Health Checks**: Kubernetes-compatible liveness, readiness, and startup probes
- **Zero Setup**: No tokens, authentication, or environment configuration required

## Quick Start

### Running Locally with dotnet run (Development Mode - Recommended)

**No setup required!** The service includes sample mocks and uses local file system by default.

```bash
# 1. Clone the repository
git clone https://github.com/busadave13/mockery.git
cd mockery

# 2. Navigate to the application directory
cd src/Mockery

# 3. Run the service (Development mode is automatic)
dotnet run --urls "http://localhost:8080"

# 4. Test with included sample mocks
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock
curl -i -H "X-Mock-ID: Products/hydrate" http://localhost:8080/api/mock
```

**Sample mocks included:**
- `FooBar/1234` - JSON response with custom headers
- `FooBar/5678` - HTML response
- `Products/hydrate` - Product catalog
- `Products/error` - Error response

**Add your own mocks:** Simply create files in the `mocks/` directory (at repository root) while the service is running!

```bash
# Navigate back to repository root
cd ../..

# Create a new mock
mkdir -p mocks/MyService
echo '{"status":"success"}' > mocks/MyService/test.json

# Test it immediately (no restart needed)
curl -i -H "X-Mock-ID: MyService/test" http://localhost:8080/api/mock
```

**Important:**
- Always run `dotnet run` from the `src/Mockery` directory
- Development mode is automatic when running locally
- Mock files are located at repository root: `mocks/{ServiceName}/{FileId}.{extension}`

### Running with Docker Compose (Development Mode with Observability)

**Docker Desktop uses Development mode with local mocks and includes Aspire Dashboard for observability.**

```bash
# 1. Build and run with docker-compose (includes Aspire Dashboard)
docker-compose up --build

# 2. Access services:
# - Mockery API: http://localhost:8080
# - Aspire Dashboard: http://localhost:18888

# 3. Test the endpoints (using local mocks from ./mocks folder)
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock
curl -i -H "X-Mock-ID: Products/hydrate" http://localhost:8080/api/mock

# 4. View telemetry in Aspire Dashboard
# Open http://localhost:18888 in your browser to see:
# - Structured logs from Mockery service
# - Distributed traces of HTTP requests
# - Metrics (request counts, durations, etc.)

# 5. Check container logs
docker-compose logs -f mockery

# 6. Stop and remove
docker-compose down
```

**Services Included:**
- **mockery**: Main application service (port 8080)
- **aspire-dashboard**: Observability dashboard (port 18888)

**Features:**
- Uses Development environment (local mocks from `./mocks` folder)
- Local mocks are mounted read-only into the container
- Changes to local mock files are picked up immediately (no restart needed)
- OpenTelemetry exports logs, metrics, and traces to Aspire Dashboard

### Using Pre-built Docker Images

Pull and run the latest published image from GitHub Container Registry:

```bash
# Pull latest version
docker pull ghcr.io/busadave13/mockery:latest

# Run the image (Git repository configured in appsettings.Production.json)
docker run -d --name mockery -p 8080:8080 \
  -v mockery-data:/app/mocks \
  ghcr.io/busadave13/mockery:latest

# Or use a specific version
docker pull ghcr.io/busadave13/mockery:1.0.0
docker run -d --name mockery -p 8080:8080 \
  -v mockery-data:/app/mocks \
  ghcr.io/busadave13/mockery:1.0.0

# Stop and remove
docker stop mockery && docker rm mockery
```

**Note:** Pre-built images use the Git configuration baked into `appsettings.Production.json` at build time. To use a custom Git repository, build your own image with updated configuration.

### Deploying to Kubernetes with Helm

Install Mockery using the Helm chart published to GitHub Container Registry:

```bash
# Install from OCI registry
helm install mockery oci://ghcr.io/busadave13/helm/mockery \
  --version 1.0.0 \
  --namespace dev \
  --create-namespace

# Customize with values file (optional)
helm install mockery oci://ghcr.io/busadave13/helm/mockery \
  --version 1.0.0 \
  --namespace dev \
  --values my-values.yaml

# Upgrade existing installation
helm upgrade mockery oci://ghcr.io/busadave13/helm/mockery \
  --version 1.0.0 \
  --namespace dev

# Uninstall
helm uninstall mockery --namespace dev
```

**Key Helm Configuration Options:**

```yaml
# Example my-values.yaml
config:
  aspnetcoreEnvironment: "Production"  # Always use Production for Kubernetes

persistence:
  enabled: true  # Required for Git repository storage
  size: 1Gi
  storageClass: ""  # Use cluster default

replicaCount: 2

resources:
  limits:
    cpu: 500m
    memory: 512Mi
  requests:
    cpu: 100m
    memory: 256Mi
```

For more Helm configuration options, see [charts/mockery/values.yaml](charts/mockery/values.yaml).

## Usage

### Making Requests

```bash
# Single mock ID
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock

# Multiple mock IDs (random selection)
curl -i -H "X-Mock-ID: FooBar/1234,FooBar/5678" http://localhost:8080/api/mock

# With custom status code
curl -i -H "X-Mock-ID: Products/error" -H "X-Mock-StatusCode: 500" http://localhost:8080/api/mock

# When running in kubernetes with custom host header
curl -i -H "X-Mock-ID: test/test" -H "Host: mockery.local.com" http://mockery.local.com/api/mock
```

### Mock Repository Structure

```
mocks/
├── FooBar/
│   ├── 1234.json
│   ├── 1234.headers.json        # Optional custom headers
│   └── 5678.html
├── Products/
│   ├── hydrate.json
│   ├── hydrate.headers.json
│   └── error.json
```

### Mock ID Format

- Format: `{ServiceName}/{FileId}`
- Example: `FooBar/1234`, `Products/hydrate`
- Service name must match folder name (case-sensitive)
- File extension determines Content-Type

### Headers File Format

Optional `.headers.json` file alongside mock file:

```json
{
  "X-Custom-Header": "CustomValue",
  "Cache-Control": "no-cache"
}
```

## Configuration

### Repository Modes

Mockery supports two storage modes configured via `appsettings.json`:

#### Local Mode (Development)

**Configuration in `appsettings.Development.json`:**
```json
{
  "MockRepository": {
    "Type": "Local",
    "LocalPath": "../.."
  }
}
```

**Characteristics:**
- No Git operations or dependencies
- Direct file system access from `mocks/` directory at repository root
- Changes picked up immediately (no restart needed)
- No environment variables or authentication required
- Perfect for local development with `dotnet run`

#### Git Mode (Production/Docker)

**Configuration in `appsettings.Production.json`:**
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

**Characteristics:**
- Full Git version control
- Automatic clone on startup
- Pull latest changes on restart
- All configuration in appsettings (no environment variables)
- Required for Docker/production deployments
- Ideal for production/staging environments

### Rate Limiting

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

## Development

### Building and Testing

```bash
# Restore packages and build
dotnet restore
dotnet build

# Run all tests (44 comprehensive tests)
dotnet test

# Run with verbose output
dotnet test --verbosity normal
```

**Test Coverage:**
- **Controllers**: MockController functionality, header validation, status codes
- **Services**: ContentTypeResolver and MockService with various scenarios  
- **Repository Layer**: Both GitMockRepository and LocalFileMockRepository
- **All edge cases**: Error handling, file operations, initialization

### Project Structure

```
Mockery/
├── mocks/                                 # Sample mocks (development)
│   ├── FooBar/
│   │   ├── 1234.json
│   │   ├── 1234.headers.json
│   │   └── 5678.html
│   └── Products/
│       ├── hydrate.json
│       └── error.json
├── docker-compose.yml                     # Docker Compose with Aspire Dashboard
├── Dockerfile                            # Production Docker image
├── NuGet.config                          # NuGet configuration (public sources only)
└── src/
    ├── Mockery/                           # Main application
    │   ├── Controllers/                   # API controllers
    │   ├── BusinessLogic/                # Service layer
    │   ├── Repository/                   # Mock repository implementations
    │   │   ├── FileSystemMockRepositoryBase.cs  # Shared file access logic
    │   │   ├── GitMockRepository.cs      # Git-based implementation
    │   │   └── LocalFileMockRepository.cs # Local file system implementation
    │   ├── Services/                     # Supporting services
    │   ├── Middleware/                   # Rate limiting middleware
    │   ├── Models/                       # Domain models
    │   ├── Configuration/                # Configuration classes
    │   ├── appsettings.json              # Base configuration
    │   ├── appsettings.Development.json  # Local development config
    │   └── appsettings.Production.json   # Docker/production config
    └── Mockery.Test/                     # Unit tests (44 tests)
        ├── Controllers/
        ├── Repository/
        ├── Services/
        └── ...
```

## Health Checks

- `GET /health/live`: Liveness probe (application is running)
- `GET /health/ready`: Readiness probe
  - **Local Mode**: Checks if `mocks/` directory exists at repository root
  - **Git Mode**: Checks if Git repository is accessible
- `GET /health/startup`: Startup probe (repository initialization complete)

## Observability

Mockery includes built-in OpenTelemetry integration for comprehensive observability.

### OpenTelemetry Features

- **Logs**: Structured logging with OpenTelemetry exporters
- **Traces**: Distributed tracing with automatic HTTP instrumentation
- **Metrics**: Request counts, durations, and custom application metrics (exposed at `/metrics` endpoint)
- **Package**: Uses `Shared.K8.Common` v1.0.1 NuGet package from public NuGet.org

### Development Environment (Aspire Dashboard)

When running with Docker Compose, telemetry is exported to Aspire Dashboard:

```bash
# Start services (includes Aspire Dashboard)
docker-compose up --build

# Access Aspire Dashboard
open http://localhost:18888

# View metrics endpoint
curl http://localhost:8080/metrics
```

**Dashboard Features:**
- **Structured Logs**: View all application logs with filtering and search
- **Traces**: Visualize distributed traces across HTTP requests
- **Metrics**: Monitor request rates, latencies, and custom metrics
- **Resources**: See service information and health status

**Configuration:**
- Environment variable: `OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889`
- Set automatically in `docker-compose.yml`

### Production Environment (Kubernetes)

In production, telemetry is exported via OTLP to your observability backend:

**Default Configuration:**
- OTLP endpoint: `http://aspire.monitor.svc.cluster.local:18889`
- Configurable via Helm chart `values.yaml` (`config.otlpEndpoint`)
- Metrics endpoint `/metrics` available for Prometheus scraping

**Customizing OTLP Endpoint:**

```yaml
# Example Helm values.yaml
config:
  otlpEndpoint: "http://your-otel-collector:4317"
```

### Configuration

OpenTelemetry is configured in `Program.cs` with environment variable support:

```csharp
// Add OpenTelemetry observability with custom endpoint override
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
if (!string.IsNullOrEmpty(otlpEndpoint))
{
    var customEndpoints = new List<OtlpEndpoints>
    {
        new OtlpEndpoints(
            builder.Environment.EnvironmentName,
            otlpEndpoint,
            otlpEndpoint,
            otlpEndpoint)
    };
    builder.AddObservability(endpointOverrides: customEndpoints);
}
else
{
    builder.AddObservability();
}

// Add OpenTelemetry observability middleware
app.UseObservability();
```

**Environment Variables:**
- `OTEL_EXPORTER_OTLP_ENDPOINT`: Custom OTLP endpoint URL (optional)
- If not set, uses package defaults based on environment

## CI/CD Pipeline

The project includes a single, streamlined GitHub Actions workflow (`publish-docker-helm.yml`) that:

1. **Tests**: Runs all 44 unit tests
2. **Builds**: Compiles the application and Docker image
3. **Publishes**: Pushes Docker image and Helm chart to GitHub Container Registry

**Triggers:**
- Pull request merges to main branch
- Manual workflow dispatch

**Features:**
- No authentication setup required
- Automatic versioning with GitVersion
- OCI-based Helm chart publishing
- Comprehensive build artifacts

## Architecture

### Repository Pattern with Strategy

Mockery uses the Strategy pattern to support different storage backends:

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

**Key Components:**

- **`FileSystemMockRepositoryBase`**: Abstract base class containing shared file lookup logic (`FindMockFileAsync`, `FindHeadersFileAsync`)
- **`GitMockRepository`**: Git-specific implementation with clone/pull operations using LibGit2Sharp
- **`LocalFileMockRepository`**: Simple file system implementation with no Git dependencies
- **Dependency Injection**: Repository implementation selected at startup based on `MockRepository.Type` configuration

**Benefits:**

- Clean separation between Git and file system concerns
- No Git dependencies in local development mode
- Easy to add new storage backends (e.g., Azure Blob, S3)
- Shared file lookup logic prevents code duplication

## Dependencies

### Core Dependencies
- **.NET 9.0**: Latest .NET framework
- **LibGit2Sharp**: Git operations for production mode
- **Shared.K8.Common v1.0.1**: OpenTelemetry observability with endpoint override support (public NuGet package)

### Development Dependencies
- **xUnit**: Unit testing framework
- **Moq**: Mocking framework for tests
- **FluentAssertions**: Assertion library
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing

**All dependencies are public packages from NuGet.org - no private repositories or authentication required.**

## License

GPL-3.0 License. See [LICENSE](LICENSE) for details.
