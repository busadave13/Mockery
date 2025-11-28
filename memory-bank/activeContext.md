# Active Context: Mockery

## Current Session

**Date**: November 28, 2025
**Focus**: Remove rate limiting middleware

## Recent Changes

### Rate Limiting Removed (November 28, 2025)
Removed rate limiting middleware and all related configuration:
- **Deleted**: `src/Mockery/Middleware/RateLimitingMiddleware.cs`
- **Deleted**: `src/Mockery/Configuration/RateLimitingOptions.cs`
- **Modified**: `src/Mockery/Program.cs` - Removed middleware registration and configuration
- **Modified**: `src/Mockery/appsettings.json` - Removed RateLimiting section
- **Modified**: `src/Mockery/appsettings.Development.json` - Removed RateLimiting section
- **Modified**: `src/Mockery/appsettings.Production.json` - Removed RateLimiting section

### Subfolder Support Added (November 26, 2025)
Added support for subfolders in mock ID paths:
- **Before**: Only `ServiceName/FileId` format (e.g., `FooBar/1234`)
- **After**: Supports arbitrary depth (e.g., `FooBar/Staging/1234`, `FooBar/Staging/Private/test`)

**Files Modified:**
- `src/Mockery/BusinessLogic/MockService.cs` - Changed parsing to split on last `/` instead of first
- `src/Mockery.Test/Services/MockServiceTests.cs` - Added 5 new tests for subfolder scenarios
- `mocks/README.md` - Updated documentation with subfolder examples

**Technical Details:**
- Changed from `Split('/', 2)` to `LastIndexOf('/')` for parsing mock IDs
- The last segment after the final `/` is always the FileId
- Everything before it is the path (can include multiple subfolders)

### Memory Bank Created (November 25, 2025)
Initialized the memory bank documentation structure with:
- `projectbrief.md` - High-level project overview
- `productContext.md` - Product goals and user experience
- `systemPatterns.md` - Architecture and design patterns
- `techContext.md` - Technical stack and dependencies
- `activeContext.md` - Current session context (this file)
- `progress.md` - Task tracking and project status

## Current State

### Application Status
- **Fully functional**: Mock API service operational
- **Two repository modes**: Local file and Git-based
- **Observability**: OpenTelemetry integrated
- **Deployment ready**: Docker and Helm charts available

### Code Quality
- Unit tests in place for:
  - Controllers (`MockControllerTests.cs`)
  - Repositories (`GitMockRepositoryTests.cs`, `LocalFileMockRepositoryTests.cs`)
  - Services (`ContentTypeResolverTests.cs`, `GitRepositoryRefreshServiceTests.cs`, `MockServiceTests.cs`)
- Using xUnit, Moq, and FluentAssertions

## Active Development Areas

*No active development tasks at this time. This section will be updated as work progresses.*

## Quick Reference

### Run Locally
```bash
dotnet run --project src/Mockery
```

### Test API
```bash
curl -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock
```

### Run Tests
```bash
dotnet test
```

## Session Notes

*Session notes will be added here during development work.*

---

## Context for Next Session

When resuming work on this project:
1. Review `progress.md` for pending tasks
2. Check this file for any in-progress work
3. Review recent Git commits for latest changes
