# Active Context - Mockery

## Current State

The project is a mature, well-documented ASP.NET Core 9.0 mock server with:
- **44 comprehensive unit tests** covering all components
- **Dual storage modes** (Local and Git) fully implemented
- **Full OpenTelemetry observability** integrated
- **Helm chart** for Kubernetes deployment
- **CI/CD pipeline** via GitHub Actions

## Recent Changes

Based on the design document version history:
- **v3.4 (2025-12-14)**: Updated Docker Compose port mapping from 3000 to 5500, moved memory bank to `.clinerules/memory-bank`
- **v3.3 (2025-12-13)**: Removed rate limiting references, updated project structure
- **v3.2 (2025-11-25)**: Removed `X-Mock-StatusCode` header (replaced by `.status.json` files), added OpenTelemetry, updated port to 8080, updated LibGit2Sharp to 0.30.0

## Active Work Areas

### Current Focus
- Memory bank initialization
- Project documentation and context preservation

### Repository Structure
```
Mockery/
├── .clinerules/memory-bank/  # Project memory (this directory)
├── .docs/                    # Design documentation
│   └── mockery-design.md     # Technical design document (v3.4)
├── charts/mockery/           # Helm chart for Kubernetes
├── mocks/                    # Sample mock files
├── src/
│   ├── Mockery/              # Main application
│   └── Mockery.Test/         # Unit tests (44 tests)
├── docker-compose.yml        # Local development with Aspire Dashboard
├── Dockerfile                # Production container
└── README.md                 # User-facing documentation
```

## Key Interfaces

### API Endpoint
- `GET /api/mock` with `X-Mock-ID` header
- Health checks: `/health/live`, `/health/ready`, `/health/startup`
- Metrics: `/metrics` (Prometheus format)

### Configuration
- `appsettings.Development.json` - Local mode (file system)
- `appsettings.Production.json` - Git mode (repository)

## Environment Details

| Aspect | Value |
|--------|-------|
| **Framework** | .NET 9.0 / ASP.NET Core 9.0 |
| **Port** | 8080 |
| **Test Count** | 44 tests |
| **Repository** | https://github.com/busadave13/Mockery.git |

## Open Questions / Considerations

None currently documented.

## Next Steps

1. Complete memory bank initialization
2. Continue with any pending development tasks
