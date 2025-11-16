# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Mockery is a Git-based REST API service for serving HTTP mock responses. It provides a single GET endpoint that retrieves mock files from a Git repository, enabling teams to manage mocks through standard Git workflows (commits, pull requests, version control). The service is built with ASP.NET Core 9.0+ and uses LibGit2Sharp for Git operations, with no database or authentication required.

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

### Running Locally
```bash
# Run application from src/Mockery
cd src/Mockery
dotnet run

# Application will listen on http://localhost:8080 by default
```

### Docker Commands
```bash
# Build Docker image
docker build -t dasacr.azurecr.io/mockery:latest -f src/Mockery/Dockerfile .

# Run container
docker run -d -p 8080:8080 \
  -e GIT_REPOSITORY_URL="https://github.com/your-org/mockery-mocks.git" \
  -e GIT_BRANCH="main" \
  -e GIT_CLONE_PATH="/app/mocks" \
  dasacr.azurecr.io/mockery:latest
```

### Testing Mock Endpoints
```bash
# Single mock ID (default 200 OK)
curl -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock

# Multiple mock IDs (random selection)
curl -H "X-Mock-ID: FooBar/1234,FooBar/5678,Products/9012" http://localhost:8080/api/mock

# With custom status code
curl -H "X-Mock-ID: Products/error" -H "X-Mock-StatusCode: 500" http://localhost:8080/api/mock
```

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
- `IGitMockRepository` interface and `GitMockRepository` implementation
- Direct file lookup using path: `mocks/{ServiceName}/{FileId}.*`
- Optional headers file lookup: `mocks/{ServiceName}/{FileId}.headers.json`
- Git operations: clone, pull/refresh via LibGit2Sharp

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

### Configuration and Environment Variables

**Required Environment Variables:**
- `GIT_REPOSITORY_URL`: URL of Git repository containing mock files
- `GIT_BRANCH`: Git branch to use (default: `main`)
- `GIT_CLONE_PATH`: Local file system path for repository clone (e.g., `/app/mocks`)
- `GIT_ACCESS_TOKEN`: Personal access token for private repositories (optional for public repos)

**Optional Configuration (appsettings.json):**
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
    └── GitMockRepositoryTests.cs
```

**Unit Testing:** Use Moq to mock `IGitMockRepository`, test status code semantics and random selection

**Integration Testing:** Use `WebApplicationFactory<Program>` for end-to-end testing with temporary Git repository

### Health Check Endpoints

- **GET /health/live**: Liveness probe (application is running)
- **GET /health/ready**: Readiness probe (Git repository accessible)
- **GET /health/startup**: Startup probe (initial Git clone completed)

Used by Kubernetes/container orchestrators for health monitoring.

## Deployment

### Azure Container Apps

**GitHub Actions Workflow:** `.github/workflows/build-deploy.yml`

**Jobs:**
1. Build and Test:
   - Restore: `dotnet restore src/Mockery/Mockery.csproj`
   - Build: `dotnet build src/Mockery/Mockery.csproj -c Release`
   - Test: `dotnet test src/Mockery.Test/Mockery.Test.csproj`
   - Push to `dasacr.azurecr.io/mockery`

2. Deploy to Azure Container App `mockery` in resource group `mockery`

**Required Secrets:**
- `MOCKERY_AZURE_CREDENTIALS`
- `MOCKERY_REGISTRY_USERNAME`
- `MOCKERY_REGISTRY_PASSWORD`

## Common Development Patterns

### Adding a New Mock

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
2. Implement in `GitMockRepository` class
3. Use LibGit2Sharp for Git operations
4. Use direct file path lookup: `mocks/{ServiceName}/{FileId}.*`
5. Handle file not found scenarios gracefully
