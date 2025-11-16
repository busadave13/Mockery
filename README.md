# Mockery

A Git-based REST API service for serving HTTP mock responses. Mockery enables teams to manage mocks through standard Git workflows (commits, pull requests, version control).

## Features

- **Git-Based Storage**: Mocks stored as files in a Git repository with full version control
- **Single GET Endpoint**: Simple API with header-based mock selection
- **Random Selection**: Support for multiple mock IDs with random selection
- **Custom Headers**: Optional headers files for custom HTTP response headers
- **Status Code Control**: Dynamic status code behavior via request headers
- **Rate Limiting**: Dual-strategy rate limiting (per-IP and global)
- **No Authentication**: Designed for development/testing environments
- **Health Checks**: Kubernetes-compatible liveness, readiness, and startup probes

## Quick Start

### Running with Docker

```bash
docker build -t mockery:latest .
docker run -d -p 8080:8080 \
  -e GIT_REPOSITORY_URL="https://github.com/your-org/mockery-mocks.git" \
  -e GIT_BRANCH="main" \
  -e GIT_CLONE_PATH="/app/mocks" \
  mockery:latest
```

### Running Locally

```bash
# Set environment variables
export GIT_REPOSITORY_URL="https://github.com/your-org/mockery-mocks.git"
export GIT_BRANCH="main"
export GIT_CLONE_PATH="/tmp/mocks"

# Build and run
cd src/Mockery
dotnet restore
dotnet build
dotnet run
```

## Usage

### Making Requests

```bash
# Single mock ID
curl -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock

# Multiple mock IDs (random selection)
curl -H "X-Mock-ID: FooBar/1234,FooBar/5678" http://localhost:8080/api/mock

# With custom status code
curl -H "X-Mock-ID: Products/error" -H "X-Mock-StatusCode: 500" http://localhost:8080/api/mock
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

### Environment Variables

- `GIT_REPOSITORY_URL`: URL of Git repository containing mocks (required)
- `GIT_BRANCH`: Git branch to use (default: `main`)
- `GIT_CLONE_PATH`: Local path for repository clone (default: `/app/mocks`)
- `GIT_ACCESS_TOKEN`: Access token for private repositories (optional)

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
src/
├── Mockery/                    # Main application
│   ├── Controllers/           # API controllers
│   ├── BusinessLogic/        # Service layer
│   ├── Repository/           # Git repository access
│   ├── Services/            # Supporting services
│   ├── Middleware/          # Rate limiting middleware
│   ├── Models/             # Domain models
│   └── Configuration/      # Configuration classes
└── Mockery.Test/          # Unit tests
```

## Health Checks

- `GET /health/live`: Liveness probe
- `GET /health/ready`: Readiness probe (Git repository accessible)
- `GET /health/startup`: Startup probe (initial Git clone completed)

## License

MIT
