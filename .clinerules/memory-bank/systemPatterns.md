# System Patterns - Mockery

## Architecture Overview

Mockery follows a **three-layer architecture** with pluggable storage:

```
┌─────────────────────────────────────────────────────────┐
│                   Presentation Layer                     │
│  MockController, Health Checks, Swagger, Prometheus     │
├─────────────────────────────────────────────────────────┤
│                  Business Logic Layer                    │
│           MockService, ContentTypeResolver               │
├─────────────────────────────────────────────────────────┤
│                    Repository Layer                      │
│  IGitMockRepository → FileSystemMockRepositoryBase      │
│         ├── GitMockRepository (Production)              │
│         └── LocalFileMockRepository (Development)       │
└─────────────────────────────────────────────────────────┘
```

## Design Patterns

### 1. Strategy Pattern (Repository Layer)

The repository layer uses the Strategy pattern to support different storage backends:

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

**Implementation:**
- `IGitMockRepository` - Interface defining mock retrieval operations
- `FileSystemMockRepositoryBase` - Abstract base with shared file operations
- `GitMockRepository` - Git-specific implementation (clone, pull, LibGit2Sharp)
- `LocalFileMockRepository` - Simple file system implementation

**Benefits:**
- Clean separation between Git and file system concerns
- No Git dependencies in local development mode
- Easy to add new storage backends (Azure Blob, S3, etc.)
- Shared file lookup logic prevents code duplication

### 2. Dependency Injection

All components use constructor injection via ASP.NET Core DI:

```csharp
// Program.cs - Repository selection based on configuration
if (mockRepositorySettings.Type == "Git")
{
    services.AddSingleton<IGitMockRepository, GitMockRepository>();
    services.AddHostedService<GitRepositoryRefreshService>();
}
else
{
    services.AddSingleton<IGitMockRepository, LocalFileMockRepository>();
}
```

### 3. Background Service Pattern

`GitRepositoryRefreshService` extends `BackgroundService` for periodic Git refresh:

```csharp
public class GitRepositoryRefreshService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _repository.RefreshAsync();
            await Task.Delay(_refreshInterval, stoppingToken);
        }
    }
}
```

## Code Organization

### Project Structure
```
src/Mockery/
├── Controllers/           # HTTP layer (MockController)
├── BusinessLogic/         # Service layer (MockService)
├── Repository/            # Data access layer
├── Models/                # Domain models (MockFileResult)
├── Services/              # Supporting services
├── Configuration/         # Settings classes
├── Extensions/            # Extension methods
└── Program.cs             # Application entry point
```

### Separation of Concerns

| Layer | Responsibility | Does NOT |
|-------|---------------|----------|
| **Controller** | Parse HTTP headers, set HTTP responses | Contain business logic, access repository |
| **Service** | Business rules, random selection, file lookup coordination | Access HttpContext, parse HTTP headers |
| **Repository** | File operations, Git operations | Contain business rules |

## Key Conventions

### Mock File Naming
- Content files: `{ServiceName}/{FileId}.{extension}`
- Headers files: `{ServiceName}/{FileId}.headers.json`
- Status files: `{ServiceName}/{StatusCode}.status.json`

### Configuration Structure
```json
{
  "MockRepository": {
    "Type": "Local|Git",
    "LocalPath": "...",
    "Git": {
      "RepositoryUrl": "...",
      "Branch": "main",
      "ClonePath": "/app/mocks",
      "AccessToken": ""
    }
  }
}
```

### Error Handling
- `400 Bad Request` - Missing or invalid `X-Mock-ID` header
- `404 Not Found` - Mock file not found
- `500 Internal Server Error` - Unhandled exception

## Testing Patterns

### Test Organization
```
src/Mockery.Test/
├── Controllers/           # Controller tests with mocked services
├── Services/              # Service tests with mocked repository
└── Repository/            # Repository tests with file system mocks
```

### Mocking Strategy
- Use Moq for interface mocking
- Use FluentAssertions for readable assertions
- Each layer tested in isolation with mocked dependencies

## Performance Considerations

### File Caching
- OS-level file caching for frequently accessed mocks
- No application-level caching (potential future enhancement)

### Thread Safety
- `SemaphoreSlim` for thread-safe repository operations
- `Random.Shared` for thread-safe random selection

### Async/Await
- All file I/O operations are async
- Non-blocking Git refresh via background service

## Observability Patterns

### OpenTelemetry Integration
```csharp
// Extension method pattern for configuration
public static class OpenTelemetryExtensions
{
    public static void AddObservability(this WebApplicationBuilder builder)
    {
        // Configure logging, metrics, tracing
    }
    
    public static void UseObservability(this WebApplication app)
    {
        // Configure Prometheus endpoint
    }
}
```

### Health Check Pattern
- `/health/live` - Always healthy if running
- `/health/ready` - Repository accessible
- `/health/startup` - Initialization complete
