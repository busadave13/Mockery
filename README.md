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

### Running with Docker (Production Mode)

**For production deployments, build and run the Docker image:**

```bash
# 1. Build Docker image
docker build -t mockery:latest .

# 2. Run the container (uses Git configuration from appsettings.Production.json)
docker run -d --name mockery -p 8080:8080 \
  -v mockery-data:/app/mocks \
  mockery:latest

# 3. Test the endpoints
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock

# 4. Stop and remove
docker stop mockery && docker rm mockery
```

**Note:** The Docker image uses Git mode by default, configured in [appsettings.Production.json](src/Mockery/appsettings.Production.json)

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
├── mocks/                                       # Sample mocks (development)
│   ├── FooBar/
│   │   ├── 1234.json
│   │   ├── 1234.headers.json
│   │   └── 5678.html
│   └── Products/
│       ├── hydrate.json
│       └── error.json
├── docker-compose.yml                           # Aspire Dashboard for local development
├── Dockerfile                                  # Production Docker image
├── NuGet.config                                # NuGet configuration (public sources only)
└── src/
    ├── Mockery/                                 # Main application
    │   ├── Controllers/                         # API controllers
    │   ├── BusinessLogic/                      # Service layer
    │   ├── Repository/                         # Mock repository implementations
    │   │   ├── FileSystemMockRepositoryBase.cs  # Shared file access logic
    │   │   ├── GitMockRepository.cs            # Git-based implementation
    │   │   └── LocalFileMockRepository.cs       # Local file system implementation
    │   ├── Services/                           # Supporting services
    │   ├── Middleware/                         # Rate limiting middleware
    │   ├── Models/                             # Domain models
    │   ├── Configuration/                      # Configuration classes
    │   ├── Extensions/                         # Extension methods
    │   │   └── OpenTelemetryExtensions.cs      # OpenTelemetry configuration
    │   ├── Properties/
    │   │   └── launchSettings.json             # Launch profiles for IDE
    │   ├── appsettings.json                    # Base configuration
    │   ├── appsettings.Development.json        # Local development config
    │   └── appsettings.Production.json         # Docker/production config
    └── Mockery.Test/                           # Unit tests (44 tests)
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

## Observability & Telemetry

Mockery includes comprehensive OpenTelemetry integration for observability, providing distributed tracing, metrics, and structured logging.

### Features

- **Distributed Tracing**: Track requests across components
- **Metrics**: Application performance and health metrics  
- **Logging**: Structured logs with correlation IDs
- **Aspire Dashboard Integration**: Development environment includes dashboard for telemetry visualization
- **Prometheus Support**: Metrics endpoint for Prometheus scraping

### Development Setup

#### Method 1: Using Launch Profiles (Recommended)

The project includes launch profiles in [launchSettings.json](src/Mockery/Properties/launchSettings.json) that work with any IDE:

**VS Code:**
1. Start the Aspire Dashboard:
   ```bash
   docker compose up -d
   ```

2. In VS Code, press `F5` or go to **Run and Debug** → **Mockery**

**Command Line:**
```bash
# Start Aspire Dashboard
docker compose up -d

# Run with telemetry (now the default profile)
cd src/Mockery
dotnet run
```

**Other IDEs:**
The `launchSettings.json` file is a standard .NET project file that works with:
- Visual Studio
- JetBrains Rider
- Any IDE that supports .NET launch profiles

**Profile:**
- **`"Mockery"`**: Full OpenTelemetry configuration enabled by default

#### Method 2: Manual Environment Variables

For advanced scenarios or CI/CD pipelines, first start the Aspire Dashboard:

```bash
# Start Aspire Dashboard
docker compose up -d

# Windows (PowerShell)
$env:OTEL_SERVICE_NAME = "Mockery"
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:18889"
$env:OTEL_EXPORTER_OTLP_PROTOCOL = "grpc"
cd src/Mockery
dotnet run

# Linux/macOS/WSL (Bash)
export OTEL_SERVICE_NAME="Mockery"
export OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:18889"
export OTEL_EXPORTER_OTLP_PROTOCOL="grpc"
cd src/Mockery
dotnet run
```

### Aspire Dashboard

Once both the dashboard and application are running:

1. **Aspire Dashboard**: http://localhost:18888
2. **Application**: http://localhost:8080
3. **Application Health**: http://localhost:8080/health/live

**Dashboard Features:**

📊 **Metrics**
- HTTP request rates and latencies
- System resource usage
- Custom application metrics
- Prometheus-compatible metrics endpoint

🔍 **Traces**
- End-to-end request tracing
- Service dependency mapping
- Performance bottleneck identification
- Error correlation

📝 **Logs** 
- Structured application logs
- Log correlation with traces
- Filtering and search capabilities
- Real-time log streaming

### Production Configuration

**Kubernetes/Helm:**
```yaml
# Example Helm values.yaml
config:
  otlpEndpoint: "http://your-otel-collector:4317"
```

**Environment Variables:**

| Variable | Description | Local Development | Kubernetes |
|----------|-------------|-------------------|------------|
| `OTEL_SERVICE_NAME` | Service identifier in traces | `Mockery` | `Mockery` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Telemetry endpoint URL | `http://localhost:18889` | `http://aspire.monitor.svc.cluster.local:18889` |
| `OTEL_EXPORTER_OTLP_PROTOCOL` | Export protocol | `grpc` | `grpc` |

### Configuration Architecture

**Single Source of Truth:**
OpenTelemetry configuration is handled exclusively through **environment variables**:
- **Launch Profiles**: Set environment variables for local development
- **Docker Compose**: Set environment variables for containerized deployment  
- **Manual**: Set environment variables directly

**No Configuration Files:**
The application **does not** read telemetry settings from `appsettings.json` files. This ensures:
- ✅ **Consistency**: Same configuration method across all environments
- ✅ **Standard Compliance**: Uses OpenTelemetry standard environment variables
- ✅ **Simplicity**: Single configuration approach

### Team Development Workflow

**For New Developers:**
1. Clone repository
2. Start Aspire Dashboard: `docker compose up -d`
3. Run with telemetry: `cd src/Mockery && dotnet run`
4. View telemetry at http://localhost:18888

**No additional setup required!** All configuration is included in the repository.

**Development Mode:**
- Telemetry is enabled by default in the single "Mockery" launch profile
- To run without telemetry, unset the OTEL environment variables or don't start the Aspire Dashboard

### Troubleshooting

**No Telemetry Data in Dashboard:**

1. Check if dashboard is running:
   ```bash
   curl -I http://localhost:18888
   ```

2. Verify environment variables are set:
   ```bash
   # PowerShell
   Get-ChildItem Env:OTEL_*
   
   # Bash
   env | grep OTEL_
   ```

3. Verify telemetry is enabled (default profile includes it):
   ```bash
   # Telemetry is enabled by default with dotnet run
   dotnet run
   ```

**Dashboard Shows "Unhealthy":**
This is often due to browser storage issues and doesn't affect functionality. You can:
- Clear browser cache and cookies for localhost:18888
- Use an incognito/private browsing window
- Restart the dashboard container

**Connection Refused Errors:**

1. Ensure dashboard is running:
   ```bash
   docker ps | grep aspire-dashboard
   ```

2. Check port availability:
   ```bash
   netstat -an | findstr 18889  # Windows
   netstat -an | grep 18889     # Linux/macOS
   ```

3. Restart the dashboard:
   ```bash
   docker compose restart
   ```

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
- **OpenTelemetry**: Native OpenTelemetry packages for observability
  - OpenTelemetry.Exporter.OpenTelemetryProtocol
  - OpenTelemetry.Exporter.Prometheus.AspNetCore
  - OpenTelemetry.Extensions.Hosting
  - OpenTelemetry.Instrumentation.AspNetCore
  - OpenTelemetry.Instrumentation.Http

### Development Dependencies
- **xUnit**: Unit testing framework
- **Moq**: Mocking framework for tests
- **FluentAssertions**: Assertion library
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing

**All dependencies are public packages from NuGet.org - no private repositories or authentication required.**

## License

GPL-3.0 License. See [LICENSE](LICENSE) for details.
