# Mockery

REST API service for serving HTTP mock responses with support for both local file system (development) and Git-based storage (production). Mockery enables:

- **Local Development**: Instant testing with file system-based mocks (no Git setup required)
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
- **No Authentication**: Designed for development/testing environments
- **Health Checks**: Kubernetes-compatible liveness, readiness, and startup probes

## Quick Start

### Running Locally with dotnet run (Development Mode - Recommended)

**No Git setup required!** The service includes sample mocks and uses local file system by default.

```bash
# 1. Clone the repository
git clone https://github.com/your-org/mockery.git
cd mockery

# 2. Navigate to the application directory
cd src/Mockery

# 3. Set environment to Development and run the service
export ASPNETCORE_ENVIRONMENT=Development
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
- **Must set** `ASPNETCORE_ENVIRONMENT=Development` before running (otherwise uses Production/Git mode)
- Mock files are located at repository root: `mocks/{ServiceName}/{FileId}.{extension}`

### Running with Docker Compose (Production - Git Mode)

**Docker deployments always use Git mode.** Configure your Git repository in `src/Mockery/appsettings.Production.json` before building.

```bash
# 1. Configure Git repository settings
# Edit src/Mockery/appsettings.Production.json:
# {
#   "MockRepository": {
#     "Type": "Git",
#     "Git": {
#       "RepositoryUrl": "https://github.com/your-org/mockery-mocks.git",
#       "Branch": "main",
#       "ClonePath": "/app/mocks",
#       "AccessToken": ""  # Add token for private repos
#     }
#   }
# }

# 2. Build and run with docker-compose
docker-compose up -d

# 3. Test the endpoints (using mocks from your Git repository)
curl -i -H "X-Mock-ID: test/test" http://localhost:8080/api/mock

# 4. Check container logs
docker-compose logs -f mockery

# 5. Stop and remove
docker-compose down
```

**Important:**
- Docker always uses Git mode (configured in `appsettings.Production.json`)
- No environment variables needed for Git configuration
- The `docker-compose.yml` includes health checks and volume persistence
- Repository is cloned on first startup and persisted in a Docker volume

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

### Publishing Docker Images to GitHub Container Registry

To publish your own Docker image to GitHub Container Registry:

```bash
# 1. Login to GitHub Container Registry
docker login ghcr.io -u busadave13

# You'll be prompted for your personal access token

# 2. Build the image with your repository URL and Git configuration
# Make sure src/Mockery/appsettings.Production.json has the correct Git repository
docker build -t ghcr.io/busadave13/mockery:latest .

# 3. Optionally tag with a version number
docker tag ghcr.io/busadave13/mockery:latest ghcr.io/busadave13/mockery:1.0.0

# 4. Push to GitHub Container Registry
docker push ghcr.io/busadave13/mockery:latest
docker push ghcr.io/busadave13/mockery:1.0.0

# 5. Verify the image was pushed
docker pull ghcr.io/busadave13/mockery:latest
```

**Important:**
- Update `src/Mockery/appsettings.Production.json` with your Git repository URL before building
- The Git configuration is baked into the image at build time
- Use semantic versioning for version tags (e.g., 1.0.0, 1.1.0)
- Always push both `latest` and a specific version tag
- Requires GitHub personal access token with `packages:write` permission

### Publishing Helm Charts to GitHub Container Registry

To publish the Helm chart as an OCI artifact to GitHub Container Registry:

```bash
# 1. Package the Helm chart
helm package charts/mockery

# This creates: mockery-1.0.0.tgz

# 2. Login to GitHub Container Registry
helm registry login ghcr.io -u busadave13

# You'll be prompted for your personal access token

# 3. Push the chart to GitHub Container Registry
helm push mockery-1.0.0.tgz oci://ghcr.io/busadave13/helm

# 4. Verify by pulling the chart
helm pull oci://ghcr.io/busadave13/helm/mockery --version 1.0.0
```

The chart will be available at: `oci://ghcr.io/busadave13/helm/mockery`

### Deploying to Kubernetes with Helm

Install Mockery using the Helm chart published to GitHub Container Registry:

```bash
# Install from OCI registry
# Note: Git repository is configured in the Docker image via appsettings.Production.json
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
# Git repository configuration is baked into the Docker image
# You only need to customize these settings if different from defaults

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
curl -i -H "X-Mock-ID: test/test" -H "Host: mockery.local.com"  http://mockery.local.com/api/mock
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
- No environment variables required
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

### Rate Limiting (appsettings.json)

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

### Building

```bash
dotnet restore
dotnet build
```

### Running Tests

```bash
dotnet test src/Mockery.Test/Mockery.Test.csproj
```

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
├── docker-compose.yml                     # Docker Compose configuration
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
    └── Mockery.Test/                     # Unit tests
        ├── Repository/
        │   ├── GitMockRepositoryTests.cs
        │   └── LocalFileMockRepositoryTests.cs
        └── ...
```

## Health Checks

- `GET /health/live`: Liveness probe (application is running)
- `GET /health/ready`: Readiness probe
  - **Local Mode**: Checks if `mocks/` directory exists at repository root
  - **Git Mode**: Checks if Git repository is accessible
- `GET /health/startup`: Startup probe (repository initialization complete)

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

## License

MIT
