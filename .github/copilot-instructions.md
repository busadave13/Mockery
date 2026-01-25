---
applyTo: '**'
description: Instructions for the Mockery .NET REST API project workspace.
---

# Copilot Instructions for Mockery

## Overview
- .NET 9 REST API that serves HTTP mock responses.
- Dual storage modes via Strategy pattern: Local file system (development) and Git-backed repository (production).
- Clear 3-layer architecture: Controllers (HTTP), BusinessLogic (orchestration), Repository (file access). See [src/Mockery](src/Mockery).

## Core Structure & Responsibilities
- Controllers: Thin HTTP layer. Key files: [src/Mockery/Controllers/MockController.cs](src/Mockery/Controllers/MockController.cs), [src/Mockery/Controllers/MocksController.cs](src/Mockery/Controllers/MocksController.cs).
- Business Logic: IMockService and MockService handle mock ID parsing, status/headers resolution, random selection. See [src/Mockery/BusinessLogic](src/Mockery/BusinessLogic).
- Repository: Strategy via IGitMockRepository with shared base FileSystemMockRepositoryBase; implementations: GitMockRepository (Git mode), LocalFileMockRepository (Local mode). See [src/Mockery/Repository](src/Mockery/Repository).
- Services: Content type mapping, Git refresh background service, metrics. See [src/Mockery/Services](src/Mockery/Services).
- Observability: OpenTelemetry setup via [src/Mockery/Extensions/OpenTelemetryExtensions.cs](src/Mockery/Extensions/OpenTelemetryExtensions.cs) with `/metrics` endpoint.

## Storage & File Semantics
- Mocks live under mocks/ with service folders and optional subpaths.
- File types:
    - `{id}.{ext}` response body (Content-Type from extension).
    - `{id}.headers.json` adds custom response headers.
    - `{statusCode}.status.json` returns HTTP status from filename; optional JSON body.
- Resolution order for `X-Mockery-Mock: Service/ID`:
    1. Status file (Service/ID.status.json) → status from filename.
    2. Content file (Service/ID.*) → 200 OK.
    3. 404 if none.
- Content types handled by ContentTypeResolver in [src/Mockery/Services](src/Mockery/Services).

## Configuration & Modes
- Local mode: [src/Mockery/appsettings.Development.json](src/Mockery/appsettings.Development.json) uses `Type=Local` and reads mocks/ at repo root.
- Git mode: [src/Mockery/appsettings.Production.json](src/Mockery/appsettings.Production.json) uses `Type=Git` with `RepositoryUrl`, `Branch`, `ClonePath`, `AccessToken`.
- Mode selection and DI wiring in [src/Mockery/Program.cs](src/Mockery/Program.cs).

## Build, Test, Run
- Build/test from repo root:
    - `dotnet restore`
    - `dotnet build`
    - `dotnet test`
- Run locally (Development mode):
    - `cd src/Mockery`
    - `dotnet run --urls "http://localhost:8080"`
- Docker:
    - Build: `docker build -t mockery:latest .`
    - Run: `docker run -d -p 8080:8080 -v mockery-data:/app/mocks mockery:latest`
- Docker Compose (local dev): `docker-compose up -d` (reads `.env` for `MOCKS_PATH`).
- Helm: chart at [charts/mockery](charts/mockery); configure `persistence`, `replicaCount`, and telemetry values.

## Observability
- OTEL environment variables drive telemetry (service name, OTLP endpoint/protocol).
- Aspire Dashboard with Docker Compose for local observability; see [src/Mockery/Properties/launchSettings.json](src/Mockery/Properties/launchSettings.json) and README.

## Health & API Surface
- Health checks: `/health/live`, `/health/ready`, `/health/startup`.
- Metrics endpoint: `/metrics`.
- Primary API: `GET /api/mock` uses header `X-Mockery-Mock: <path>/<id>[,<path>/<id>...]` with random selection if multiple.
- Mock Management API: `GET/POST/DELETE /api/mocks` for listing, creating, deleting files (Git mode commits/pushes).

## Conventions
- DI required; constructor null-checks for all dependencies.
- Nullable reference types enabled; file-scoped namespaces; async/await for I/O.
- Naming: PascalCase for classes/methods; camelCase for variables/params; interfaces prefixed with `I`.
- Tests: xUnit with Moq/FluentAssertions; AAA pattern and descriptive names. See [src/Mockery.Test](src/Mockery.Test).

## Where to Look First
- End-to-end flow: [src/Mockery/Controllers/MockController.cs](src/Mockery/Controllers/MockController.cs) → [src/Mockery/BusinessLogic/MockService.cs](src/Mockery/BusinessLogic/MockService.cs) → [src/Mockery/Repository/*](src/Mockery/Repository) → [src/Mockery/Services/ContentTypeResolver.cs](src/Mockery/Services/ContentTypeResolver.cs).
- Configuration wiring: [src/Mockery/Program.cs](src/Mockery/Program.cs).
- Example mocks: [mocks](mocks) and usage in [README.md](README.md).

---

# Mockery Workspace Instructions

## Project Overview
Mockery is a .NET 9 REST API service for serving HTTP mock responses. It supports dual storage modes: local file system (development) and Git-based storage (production).

## Tech Stack
- .NET 9 / ASP.NET Core Web API
- C# with nullable reference types enabled
- OpenTelemetry for observability
- LibGit2Sharp for Git operations
- xUnit with Moq for testing

## Project Structure
```
src/Mockery/           # Main API application
├── Controllers/       # Thin HTTP layer
├── BusinessLogic/     # Business rules and orchestration
├── Repository/        # Data access layer
├── Models/            # DTOs and response models
├── Configuration/     # Settings classes
├── Extensions/        # Extension methods
└── Services/          # Supporting services

src/Mockery.Test/      # Unit tests (xUnit)
mocks/                 # Sample mock files for local development
charts/mockery/        # Helm charts for Kubernetes deployment
```

## Architecture Rules

### Layer Responsibilities
| Layer | Does | Does NOT |
|-------|------|----------|
| **Controller** | Parse HTTP requests, set responses, call services | Contain business logic, access repositories directly |
| **Business Logic** | Business rules, orchestration, model mapping | Access HttpContext, return ActionResult |
| **Repository** | Data access, file/database operations | Contain business logic, call external APIs |

### Dependency Injection
All dependencies MUST be injected via constructor with null checks:
```csharp
public OrderService(IOrderRepository repository, ILogger<OrderService> logger)
{
    _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}
```

## Coding Standards

### Naming Conventions
- Classes/Methods/Properties: `PascalCase`
- Variables/Parameters: `camelCase`
- Private fields: `_camelCase`
- Interfaces: `I` prefix (e.g., `IOrderService`)

### Class Member Order
1. Constants → 2. Static fields → 3. Private fields → 4. Constructors → 5. Properties → 6. Public methods → 7. Private methods

### Modern C# Features (Required)
- Nullable reference types (`#nullable enable`)
- File-scoped namespaces
- Records for immutable DTOs
- Pattern matching where appropriate
- Async/await for all I/O operations

## Testing Standards

### Test Naming
Format: `{MethodName}_{Scenario}_{ExpectedOutcome}`
```csharp
[Fact]
public async Task GetOrderAsync_WithValidId_ReturnsOrder()
```

### Arrange-Act-Assert Pattern
All tests must follow AAA pattern with clear section comments.

## Git Operations

### Commit Rules
- **NEVER** commit directly to the `main` branch without first warning the user and receiving explicit approval.
- **NEVER** commit to a private branch or worktree without first getting user approval.
- **ALWAYS** create a unique, descriptive commit message based on the actual changes being committed.
- **ALWAYS** get user approval before pushing changes to a remote branch.

### Commit Message Guidelines
- Summarize the changes concisely in the subject line (50 characters or less).
- Use imperative mood (e.g., "Add feature" not "Added feature").
- Include relevant context in the body if needed.

## Build Verification

**BEFORE completing ANY code changes, run:**
```bash
dotnet build --configuration Release
dotnet test --configuration Release
```

## Running Locally
```bash
cd src/Mockery
dotnet run --urls "http://localhost:8080"
```
