---
applyTo: '**'
description: Instructions for the Mockery .NET REST API project workspace.
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