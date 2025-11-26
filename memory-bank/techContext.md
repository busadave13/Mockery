# Technical Context: Mockery

## Technology Stack

### Runtime & Framework
| Component | Version | Purpose |
|-----------|---------|---------|
| .NET | 9.0 | Runtime |
| ASP.NET Core | 9.0 | Web framework |
| C# | 13.0 (implied by .NET 9) | Language |

### Key Dependencies

#### Main Application (`src/Mockery/Mockery.csproj`)

| Package | Version | Purpose |
|---------|---------|---------|
| LibGit2Sharp | 0.30.0 | Git repository operations (clone, pull) |
| Microsoft.AspNetCore.OpenApi | 9.0.0 | OpenAPI/Swagger support |
| OpenTelemetry.Exporter.Console | 1.10.0 | Console telemetry export |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.10.0 | OTLP telemetry export |
| OpenTelemetry.Exporter.Prometheus.AspNetCore | 1.10.0-beta.1 | Prometheus metrics |
| OpenTelemetry.Extensions.Hosting | 1.10.0 | Hosting integration |
| OpenTelemetry.Instrumentation.AspNetCore | 1.10.0 | ASP.NET Core instrumentation |
| OpenTelemetry.Instrumentation.Http | 1.10.0 | HTTP client instrumentation |
| Swashbuckle.AspNetCore | 6.9.0 | Swagger UI |

#### Test Project (`src/Mockery.Test/Mockery.Test.csproj`)

| Package | Version | Purpose |
|---------|---------|---------|
| xunit | 2.9.2 | Test framework |
| xunit.runner.visualstudio | 2.8.2 | VS Test integration |
| Moq | 4.20.72 | Mocking framework |
| FluentAssertions | 6.12.2 | Assertion library |
| Microsoft.AspNetCore.Mvc.Testing | 9.0.0 | Integration testing |
| coverlet.collector | 6.0.2 | Code coverage |
| Microsoft.NET.Test.Sdk | 17.12.0 | Test SDK |

## Project Structure

```
Mockery/
├── .clinerules                    # Cline AI configuration
├── .gitignore
├── .gitversion.yml                # GitVersion configuration
├── docker-compose.yml             # Local Docker setup
├── Dockerfile                     # Container build
├── LICENSE
├── Mockery.sln                    # Solution file
├── NuGet.config                   # NuGet configuration
├── README.md
├── charts/                        # Helm charts
│   └── mockery/
│       ├── Chart.yaml
│       ├── values.yaml
│       └── templates/
│           ├── deployment.yaml
│           ├── httproute.yaml
│           ├── pvc.yaml
│           ├── secret.yaml
│           ├── service.yaml
│           └── serviceaccount.yaml
├── mocks/                         # Sample mock files
│   ├── FooBar/
│   └── Products/
├── memory-bank/                   # Project documentation
└── src/
    ├── Mockery/                   # Main application
    │   ├── appsettings.*.json     # Configuration files
    │   ├── Mockery.csproj
    │   ├── Program.cs             # Entry point
    │   ├── BusinessLogic/         # Service layer
    │   ├── Configuration/         # Options classes
    │   ├── Controllers/           # API controllers
    │   ├── Extensions/            # Extension methods
    │   ├── Middleware/            # Custom middleware
    │   ├── Models/                # Data models
    │   ├── Repository/            # Data access
    │   └── Services/              # Additional services
    └── Mockery.Test/              # Unit/integration tests
        ├── Mockery.Test.csproj
        ├── Controllers/
        ├── Repository/
        └── Services/
```

## Configuration Files

### appsettings.json Structure
```json
{
  "Logging": { ... },
  "MockRepository": {
    "Type": "Local|Git",
    "LocalPath": "./mocks",
    "Git": {
      "RepositoryUrl": "",
      "Branch": "main",
      "ClonePath": "/app/mocks",
      "AccessToken": ""
    }
  },
  "RateLimiting": {
    "Enabled": true,
    "RequestsPerInterval": 100,
    "IntervalSeconds": 60
  }
}
```

### Environment-Specific Configuration
- `appsettings.Development.json` - Local development settings
- `appsettings.Production.json` - Production settings

## Build & Run Commands

### Local Development
```bash
# Build
dotnet build

# Run
dotnet run --project src/Mockery

# Run tests
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Docker
```bash
# Build image
docker build -t mockery .

# Run container
docker-compose up

# Run with environment variables
docker run -e MockRepository__Type=Local -p 8080:8080 mockery
```

### Kubernetes/Helm
```bash
# Install chart
helm install mockery ./charts/mockery

# Install with custom values
helm install mockery ./charts/mockery -f custom-values.yaml

# Upgrade
helm upgrade mockery ./charts/mockery
```

## OpenTelemetry Configuration

Environment variables for OTLP:
```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889
OTEL_EXPORTER_OTLP_PROTOCOL=grpc  # or http/protobuf
OTEL_SERVICE_NAME=mockery
```

## Development Setup

### Prerequisites
- .NET 9.0 SDK
- Docker (optional, for containerized runs)
- Git (for repository features)

### IDE Configuration
- Visual Studio Code with C# extension
- Visual Studio 2022 17.8+
- JetBrains Rider 2024.1+

### Launch Settings (`Properties/launchSettings.json`)
Defines profiles for local development with debug settings.

## API Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/mock` | GET | Retrieve mock response |
| `/health/live` | GET | Liveness probe |
| `/health/ready` | GET | Readiness probe |
| `/health/startup` | GET | Startup probe |
| `/swagger` | GET | Swagger UI (dev only) |

## Key Technical Decisions

1. **LibGit2Sharp for Git operations**: Native Git operations without shelling out
2. **OpenTelemetry stack**: Industry-standard observability
3. **File-based mocks**: Simple, version-controllable approach
4. **Options pattern**: Type-safe configuration
5. **Background service for refresh**: Non-blocking Git sync
