# Mockery

A REST API service for serving HTTP mock responses with support for both local file system (development) and Git-based storage (production). Mockery enables:

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

### Running Locally (Development Mode - Recommended)

**No Git setup required!** The service includes sample mocks and uses local file system by default.

```bash
# 1. Clone the repository
git clone https://github.com/your-org/mockery.git
cd mockery

# 2. Run the service
cd src/Mockery
dotnet run

# 3. Test with included sample mocks
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:3000/api/mock
```

**Sample mocks included:**
- `FooBar/1234` - JSON response with custom headers
- `FooBar/5678` - HTML response
- `Products/hydrate` - Product catalog
- `Products/error` - Error response

**Add your own mocks:** Simply create files in the `.mocks/` directory while the service is running!

```bash
# Create a new mock
mkdir -p .mocks/MyService
echo '{"status":"success"}' > .mocks/MyService/test.json

# Test it immediately (no restart needed)
curl -i -H "X-Mock-ID: MyService/test" http://localhost:3000/api/mock
```

### Running with Docker

#### Development Mode (Local File System)

Run Mockery in Docker with local file system mocks (no Git required):

```bash
# 1. Build the Docker image
docker build -t mockery:latest .

# 2. Run container with volume mount to local .mocks directory
docker run -d --name mockery -p 3000:3000 \
  -v "$(pwd)/.mocks:/app/mocks/mocks" \
  -e ASPNETCORE_ENVIRONMENT=Development \
  mockery:latest

# On Windows (PowerShell or CMD), use absolute path:
docker run -d --name mockery -p 3000:3000 \
  -v "C:\path\to\Mockery\.mocks:/app/mocks/mocks" \
  -e ASPNETCORE_ENVIRONMENT=Development \
  mockery:latest

# 3. Test the endpoints
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:3000/api/mock
curl -i -H "X-Mock-ID: FooBar/5678" http://localhost:3000/api/mock
curl -i -H "X-Mock-ID: Products/hydrate" http://localhost:3000/api/mock
curl -i -H "X-Mock-ID: Products/error" -H "X-Mock-StatusCode: 500" http://localhost:3000/api/mock

# 4. Check container logs
docker logs mockery

# 5. Stop and remove container
docker stop mockery && docker rm mockery
```

**Note**: The volume mount maps your local `.mocks` directory to `/app/mocks/mocks` in the container because the service creates a nested directory structure.

#### Production Mode (Git-Based)

Run Mockery with Git repository for mocks:

```bash
docker build -t mockery:latest .
docker run -d --name mockery -p 3000:3000 \
  -e GIT_REPOSITORY_URL="https://github.com/your-org/mockery-mocks.git" \
  -e GIT_BRANCH="main" \
  -e GIT_CLONE_PATH="/app/mocks" \
  mockery:latest
```

## Usage

### Making Requests

```bash
# Single mock ID
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:3000/api/mock

# Multiple mock IDs (random selection)
curl -i -H "X-Mock-ID: FooBar/1234,FooBar/5678" http://localhost:3000/api/mock

# With custom status code
curl -i -H "X-Mock-ID: Products/error" -H "X-Mock-StatusCode: 500" http://localhost:3000/api/mock
```

### Mock Repository Structure

```
.mocks/
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

#### Local Mode (Development - Default)

**Configuration in `appsettings.Development.json`:**
```json
{
  "MockRepository": {
    "Type": "Local",
    "LocalPath": "./.mocks"
  }
}
```

**Characteristics:**
- No Git operations or dependencies
- Direct file system access
- Changes picked up immediately
- No environment variables required
- Perfect for local development and testing

#### Git Mode (Production)

**Configuration in `appsettings.Production.json` (or omit for default):**
```json
{
  "MockRepository": {
    "Type": "Git"
  }
}
```

**Required Environment Variables:**
- `GIT_REPOSITORY_URL`: URL of Git repository containing mocks (required)
- `GIT_BRANCH`: Git branch to use (default: `main`)
- `GIT_CLONE_PATH`: Local path for repository clone (default: `/app/mocks`)
- `GIT_ACCESS_TOKEN`: Access token for private repositories (optional)

**Characteristics:**
- Full Git version control
- Automatic clone on startup
- Pull latest changes on restart
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
├── .mocks/                                # Sample mocks (development)
│   ├── FooBar/
│   │   ├── 1234.json
│   │   ├── 1234.headers.json
│   │   └── 5678.html
│   └── Products/
│       ├── hydrate.json
│       └── error.json
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
    │   └── Configuration/                # Configuration classes
    └── Mockery.Test/                     # Unit tests
        ├── Repository/
        │   ├── GitMockRepositoryTests.cs
        │   └── LocalFileMockRepositoryTests.cs
        └── ...
```

## Health Checks

- `GET /health/live`: Liveness probe (application is running)
- `GET /health/ready`: Readiness probe
  - **Local Mode**: Checks if `.mocks/` directory exists
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
