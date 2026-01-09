# Active Context - Mockery

## Current State

The project is a mature, well-documented ASP.NET Core 9.0 mock server with:
- **95 comprehensive unit tests** covering all components
- **Dual storage modes** (Local and Git) fully implemented
- **Full OpenTelemetry observability** integrated
- **Helm chart** for Kubernetes deployment
- **CI/CD pipeline** via GitHub Actions
- **Simplified k6 load testing** with RPS and DURATION parameters

## Recent Changes (v4.0 - 2026-01-08)

### Throttling Removal
- **Deleted files:**
  - `src/Mockery/Middleware/ThrottlingMiddleware.cs`
  - `src/Mockery/Services/IThrottlingService.cs`
  - `src/Mockery/Services/ThrottlingService.cs`
  - `src/Mockery/Configuration/ThrottlingOptions.cs`
  - `src/Mockery/Extensions/ThrottlingExtensions.cs`
  - `src/Mockery.Test/Middleware/ThrottlingMiddlewareTests.cs`
  - `src/Mockery.Test/Services/ThrottlingServiceTests.cs`
  - `observability/` folder (Grafana dashboards, Prometheus configs)
  - `docker-compose.observability.yml`
  - `docker-compose.observability.docker.yml`
- **Updated files:**
  - `MockeryMetrics.cs` - removed throttling counters
  - `Program.cs` - removed throttling middleware registration
  - `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json` - removed Throttling section
  - `charts/mockery/values.yaml` and `templates/configmap.yaml` - removed throttling config
  - `MockControllerTests.cs` - removed throttling dependencies
  - `docs/mockery-design.md` - removed throttling references

### k6 Load Testing Simplification
- **Deleted files:**
  - `k6/config.js`
  - `k6/scripts/full-suite.js`
  - `k6/scripts/mocks-list.js`
  - `k6/scripts/mock-load.js`
- **Created files:**
  - `k6/scripts/load-test.js` - Single, simple load test with RPS and DURATION parameters
- **Updated files:**
  - `k6/README.md` - Simplified usage instructions

## Active Work Areas

### Current Focus
- Documentation updates complete
- All tests passing (95 tests)
- Ready for production use

### Repository Structure
```
Mockery/
├── memory-bank/              # Project memory (this directory)
├── docs/                     # Design documentation
│   └── mockery-design.md     # Technical design document (v4.0)
├── charts/mockery/           # Helm chart for Kubernetes
├── mocks/                    # Sample mock files
├── k6/                       # Load testing
│   ├── README.md
│   ├── results/
│   └── scripts/
│       └── load-test.js      # Simplified load test (RPS, DURATION)
├── src/
│   ├── Mockery/              # Main application
│   └── Mockery.Test/         # Unit tests (95 tests)
├── docker-compose.yml        # Local development with Aspire Dashboard
├── Dockerfile                # Production container
└── README.md                 # User-facing documentation
```

## Key Interfaces

### API Endpoints
- `GET /api/mock` with `X-Mockery-Mock` header - Retrieve mock content
- `GET /api/mocks` - List directory contents
- `POST /api/mocks` - Create mock file
- `DELETE /api/mocks` - Delete mock file
- Health checks: `/health/live`, `/health/ready`, `/health/startup`
- Metrics: `/metrics` (Prometheus format)

### Configuration
- `appsettings.Development.json` - Local mode (file system)
- `appsettings.Production.json` - Git mode (repository)

### Load Testing
```bash
# Default: 10 RPS for 30 seconds
k6 run k6/scripts/load-test.js

# Custom
k6 run -e RPS=100 -e DURATION=60s k6/scripts/load-test.js
```

## Environment Details

| Aspect | Value |
|--------|-------|
| **Framework** | .NET 9.0 / ASP.NET Core 9.0 |
| **Port** | 8080 |
| **Test Count** | 95 tests |
| **Repository** | https://github.com/busadave13/Mockery.git |
| **Version** | v4.0 |

## Open Questions / Considerations

None currently documented.

## Next Steps

No pending tasks - project is stable and ready for use.