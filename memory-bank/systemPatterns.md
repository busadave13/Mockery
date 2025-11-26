# System Patterns: Mockery

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        HTTP Request                              │
│                    (X-Mock-ID: Service/FileId)                   │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    RateLimitingMiddleware                        │
│              (Configurable requests per interval)                │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                       MockController                             │
│                    (API Endpoint: /api/mock)                     │
│         - Parses X-Mock-ID header (comma-separated)              │
│         - Delegates to MockService                               │
│         - Sets response headers and status codes                 │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                        MockService                               │
│                     (Business Logic)                             │
│         - Random selection if multiple mock IDs                  │
│         - Parses ServiceName/FileId format                       │
│         - Coordinates file lookups                               │
│         - Resolves content types                                 │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                  IGitMockRepository                              │
│                    (Repository Interface)                        │
├─────────────────────────────────────────────────────────────────┤
│  LocalFileMockRepository  │    GitMockRepository                 │
│  (Development Mode)       │    (Production Mode)                 │
│  - Reads from local disk  │    - Clones Git repo                 │
│  - mocks/ subdirectory    │    - Periodic refresh                │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                     File System                                  │
│                  (Mock Files Storage)                            │
│         - {id}.json, {id}.html (content)                         │
│         - {id}.headers.json (custom headers)                     │
│         - {statusCode}.status.json (status codes)                │
└─────────────────────────────────────────────────────────────────┘
```

## Design Patterns

### 1. Repository Pattern
**Location**: `src/Mockery/Repository/`

The repository pattern abstracts file system access:
- `IGitMockRepository` - Interface defining mock file operations
- `FileSystemMockRepositoryBase` - Abstract base with shared file reading logic
- `LocalFileMockRepository` - Local development implementation
- `GitMockRepository` - Git-based production implementation

```csharp
public interface IGitMockRepository
{
    Task InitializeAsync();
    Task RefreshAsync();
    Task<(string Content, string Extension)?> FindMockFileAsync(string serviceName, string fileId);
    Task<Dictionary<string, string>?> FindHeadersFileAsync(string serviceName, string fileId);
    Task<(int StatusCode, string? Content)?> FindStatusFileAsync(string serviceName, string fileId);
}
```

### 2. Strategy Pattern (Repository Selection)
**Location**: `src/Mockery/Program.cs`

Repository implementation is selected based on configuration:
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

### 3. Options Pattern
**Location**: `src/Mockery/Configuration/`

Configuration classes use the .NET Options pattern:
- `GitRepositoryOptions` - Git repository settings
- `MockRepositorySettings` - Repository type and paths
- `RateLimitingOptions` - Rate limiting configuration

### 4. Middleware Pattern
**Location**: `src/Mockery/Middleware/`

Custom middleware for cross-cutting concerns:
- `RateLimitingMiddleware` - Request rate limiting

### 5. Service Layer Pattern
**Location**: `src/Mockery/BusinessLogic/`

Business logic separated from controllers:
- `IMockService` / `MockService` - Mock retrieval orchestration

## Key Flows

### Mock Retrieval Flow
1. Client sends GET request to `/api/mock` with `X-Mock-ID` header
2. `MockController` parses header (supports comma-separated IDs)
3. `MockService` selects random ID if multiple provided
4. Parses `ServiceName/FileId` format
5. Repository searches for files:
   - First: `{fileId}.status.json` (for status code overrides)
   - Then: `{fileId}.headers.json` (for custom headers)
   - Finally: `{fileId}.{ext}` (for content)
6. Response assembled with status code, headers, and content

### Repository Initialization Flow
1. Application startup reads `MockRepository` configuration
2. Appropriate repository implementation registered in DI
3. `InitializeAsync()` called on startup
4. For Git mode: `GitRepositoryRefreshService` background service starts periodic refresh

## File Naming Conventions

| Pattern | Purpose | Example |
|---------|---------|---------|
| `{id}.json` | JSON mock response | `1234.json` |
| `{id}.html` | HTML mock response | `5678.html` |
| `{statusCode}.status.json` | Status code override | `504.status.json` |
| `{id}.headers.json` | Custom response headers | `1234.headers.json` |

## Configuration Structure

```json
{
  "MockRepository": {
    "Type": "Local|Git",
    "LocalPath": "./mocks",
    "Git": {
      "RepositoryUrl": "https://...",
      "Branch": "main",
      "ClonePath": "/app/mocks",
      "AccessToken": "..."
    }
  },
  "RateLimiting": {
    "Enabled": true,
    "RequestsPerInterval": 100,
    "IntervalSeconds": 60
  }
}
```

## Health Checks

Three health check endpoints:
- `/health/live` - Application liveness
- `/health/ready` - Repository accessibility
- `/health/startup` - Startup completion
