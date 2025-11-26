# Active Context: Mockery

## Current Session

**Date**: November 25, 2025
**Focus**: Memory bank initialization

## Recent Changes

### Memory Bank Created
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
