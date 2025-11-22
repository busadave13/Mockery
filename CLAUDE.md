# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Mockery is a REST API service for serving HTTP mock responses. It provides a single GET endpoint that retrieves mock files from either a local file system (development) or a Git repository (production). This enables teams to:

- **Development**: Quickly test locally with immediate mock file changes (no Git setup required)
- **Production**: Manage mocks through standard Git workflows (commits, pull requests, version control)

The service is built with ASP.NET Core 9.0+ and uses LibGit2Sharp for Git operations in production mode. No database or authentication required.

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

# Run in Development mode (uses Local repository)
dotnet run --urls "http://localhost:8080"

# Or explicitly set environment
dotnet run --urls "http://localhost:8080" --environment Development

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

### Docker Commands

**Docker Mode (Git Repository Only)**

Docker deployments always use Git mode. Configure your Git repository settings in `appsettings.Production.json` before building the image.

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

# 2. Build Docker image
docker build -t dasacr.azurecr.io/mockery:latest .

# 3. Run container
docker run -d --name mockery -p 3000:3000 dasacr.azurecr.io/mockery:latest

# 4. Test endpoints
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:3000/api/mock
curl -i -H "X-Mock-ID: Products/hydrate" http://localhost:3000/api/mock

# 5. Stop and remove container
docker stop mockery && docker rm mockery
```

### Testing Mock Endpoints
```bash
# Single mock ID (default 200 OK)
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:3000/api/mock

# Multiple mock IDs (random selection)
curl -i -H "X-Mock-ID: FooBar/1234,FooBar/5678,Products/9012" http://localhost:3000/api/mock

# With custom status code
curl -i -H "X-Mock-ID: Products/error" -H "X-Mock-StatusCode: 500" http://localhost:3000/api/mock
```

## Repository Modes

Mockery supports two repository modes:

### Local Mode (Development)

**Configuration:** Set in `appsettings.Development.json`:
```json
{
  "MockRepository": {
    "Type": "Local",
    "LocalPath": "./.mocks"
  }
}
```

**Characteristics:**
- Uses `LocalFileMockRepository` implementation
- Reads mocks directly from local file system (`.mocks/` directory)
- No Git operations (no clone, pull, or LibGit2Sharp dependencies)
- Changes to mock files are immediately available
- Ideal for local development and testing

**Setup:**
1. Create mock files in `mocks/{ServiceName}/{FileId}.{extension}` (at repository root)
2. Navigate to `cd src/Mockery`
3. Run `dotnet run --urls "http://localhost:8080"`
4. Mock files can be edited while the service is running (no restart needed)

### Git Mode (Production/Docker)

**Configuration:** Set in `appsettings.Production.json`:
```json
{
  "MockRepository": {
    "Type": "Git",
    "Git": {
      "RepositoryUrl": "https://github.com/your-org/mockery-mocks.git",
      "Branch": "main",
      "ClonePath": "/app/mocks",
      "AccessToken": "your-token-if-private-repo"
    }
  }
}
```

**Characteristics:**
- Uses `GitMockRepository` implementation
- Clones Git repository on startup
- Pulls latest changes when repository already exists
- Mocks managed through Git workflows (commits, PRs, version control)
- Required for Docker deployments
- Ideal for production, staging, and shared environments
- Configuration is done entirely through appsettings files (no environment variables)

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

**Repository Mode Configuration (appsettings.json):**
```json
{
  "MockRepository": {
    "Type": "Local",        // "Local" for development, "Git" for Docker/production
    "LocalPath": "./mocks", // Used in Local mode only
    "Git": {                // Used in Git mode only (required for Docker)
      "RepositoryUrl": "https://github.com/your-org/mockery-mocks.git",
      "Branch": "main",
      "ClonePath": "/app/mocks",
      "AccessToken": ""
    }
  }
}
```

**Important Notes:**
- Local development (`dotnet run` from `src/Mockery`) uses Local mode with `mocks/` directory
- Docker deployments always use Git mode - configure in `appsettings.Production.json`
- All configuration is done through appsettings files
- No environment variables are used or supported for Git configuration

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

### Adding a New Mock (Local Development)

For local development (default when running `dotnet run`):

1. Navigate to project root: `cd C:\Users\daveh\source\Mockery`
2. Create service folder if needed: `mkdir -p .mocks/MyService`
3. Create mock file: `echo '{"status":"success"}' > .mocks/MyService/1234.json`
4. Optionally create headers file: `echo '{"X-Custom-Header":"value"}' > .mocks/MyService/1234.headers.json`
5. Test immediately - no restart needed!

```bash
curl -i -H "X-Mock-ID: MyService/1234" http://localhost:3000/api/mock
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
