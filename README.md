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
- **Status Code Control**: Dynamic status code behavior via `.status.json` files
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

# With status file for error response
curl -i -H "X-Mock-ID: FooBar/504" http://localhost:8080/api/mock

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

## Mock File Types

Mockery supports several file types that work together to provide flexible mock responses. Each file type serves a specific purpose in constructing HTTP responses.

### Overview

| File Pattern | Purpose | Required | Example |
|--------------|---------|----------|---------|
| `{id}.{ext}` | Response body content | Yes | `1234.json`, `user.html` |
| `{id}.headers.json` | Custom HTTP headers | No | `1234.headers.json` |
| `{id}.status.json` | HTTP status code + optional body | No | `404.status.json`, `500.status.json` |

### Content Files (`{id}.{extension}`)

The primary mock file containing the response body. The file extension determines the `Content-Type` header.

**Location:** `mocks/{ServiceName}/{FileId}.{extension}`

**Supported Extensions:**
| Extension | Content-Type |
|-----------|--------------|
| `.json` | `application/json` |
| `.html` | `text/html` |
| `.xml` | `application/xml` |
| `.txt` | `text/plain` |
| `.css` | `text/css` |
| `.js` | `application/javascript` |

**Example:** `mocks/FooBar/1234.json`
```json
{
  "id": 1234,
  "name": "Sample Item",
  "status": "active"
}
```

**Usage:**
```bash
curl -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock
# Returns: HTTP 200 with JSON body
```

### Headers Files (`{id}.headers.json`)

Optional companion file that adds custom HTTP response headers. Must be named exactly like the content file with `.headers.json` suffix.

**Location:** `mocks/{ServiceName}/{FileId}.headers.json`

**Format:** JSON object with header name-value pairs

**Example:** `mocks/FooBar/1234.headers.json`
```json
{
  "X-Custom-Header": "CustomValue",
  "X-Request-ID": "abc-123-def-456",
  "Cache-Control": "no-cache, no-store",
  "X-Service-Version": "1.0.0"
}
```

**Usage:**
```bash
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock
# Returns: HTTP 200 with JSON body AND custom headers
```

**Response Headers:**
```
HTTP/1.1 200 OK
Content-Type: application/json
X-Custom-Header: CustomValue
X-Request-ID: abc-123-def-456
Cache-Control: no-cache, no-store
X-Service-Version: 1.0.0
```

**Notes:**
- Headers file is automatically discovered when requesting the matching content file
- Headers are merged with standard response headers
- If headers file doesn't exist, only standard headers are returned

### Status Files (`{statusCode}.status.json`)

Special file type that returns a specific HTTP status code based on the filename. The status code is extracted from the first part of the filename.

**Location:** `mocks/{ServiceName}/{StatusCode}.status.json`

**Format:** 
- Filename must start with a valid HTTP status code (e.g., `404`, `500`, `503`)
- File extension must be `.status.json`
- File content is optional - can be empty or contain a JSON response body

**Examples:**

**Empty status file (status code only):** `mocks/FooBar/204.status.json`
```
(empty file)
```

**Status file with body:** `mocks/FooBar/504.status.json`
```json
{"error": "Gateway Timeout", "message": "The upstream server did not respond in time"}
```

**Usage:**
```bash
# Request status file - returns HTTP 504 with JSON error body
curl -i -H "X-Mock-ID: FooBar/504" http://localhost:8080/api/mock

# Returns:
# HTTP/1.1 504 Gateway Timeout
# Content-Type: application/json
# {"error": "Gateway Timeout", "message": "The upstream server did not respond in time"}
```

```bash
# Request 204 No Content status file
curl -i -H "X-Mock-ID: FooBar/204" http://localhost:8080/api/mock

# Returns:
# HTTP/1.1 204 No Content
# (no body)
```

**Common Status File Use Cases:**
| Status Code | File Name | Use Case |
|-------------|-----------|----------|
| `400` | `400.status.json` | Bad Request errors |
| `401` | `401.status.json` | Unauthorized errors |
| `403` | `403.status.json` | Forbidden errors |
| `404` | `404.status.json` | Not Found errors |
| `500` | `500.status.json` | Internal Server Error |
| `502` | `502.status.json` | Bad Gateway |
| `503` | `503.status.json` | Service Unavailable |
| `504` | `504.status.json` | Gateway Timeout |

### Status Code Priority

When determining the HTTP status code for a response, Mockery uses the following priority (highest to lowest):

1. **`.status.json` file** - Status code from the filename
2. **Default 200 OK** - When no status is specified

### File Resolution Order

When you request `X-Mock-ID: FooBar/504`, Mockery checks for files in this order:

1. **Status file first:** `mocks/FooBar/504.status.json`
   - If found: Returns content with HTTP status from filename (504)
2. **Content file second:** `mocks/FooBar/504.json`, `504.html`, etc.
   - If found: Returns content with HTTP 200
3. **Not found:** Returns HTTP 404

### Complete Example: Error Scenario Mocking

**Directory Structure:**
```
mocks/
└── UserService/
    ├── get-user.json           # Success response (200)
    ├── get-user.headers.json   # Custom headers for success
    ├── 400.status.json         # Bad request error
    ├── 401.status.json         # Unauthorized error
    ├── 404.status.json         # User not found error
    └── 500.status.json         # Internal server error
```

**Test Different Scenarios:**
```bash
# Success case
curl -i -H "X-Mock-ID: UserService/get-user" http://localhost:8080/api/mock
# HTTP 200 with user data and custom headers

# Bad request
curl -i -H "X-Mock-ID: UserService/400" http://localhost:8080/api/mock
# HTTP 400 with error message

# Unauthorized
curl -i -H "X-Mock-ID: UserService/401" http://localhost:8080/api/mock
# HTTP 401 with auth error

# User not found
curl -i -H "X-Mock-ID: UserService/404" http://localhost:8080/api/mock
# HTTP 404 with not found message

# Server error
curl -i -H "X-Mock-ID: UserService/500" http://localhost:8080/api/mock
# HTTP 500 with error details
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
