# Progress - Mockery

## Project Status: ✅ Stable / Production Ready

The Mockery project is a mature, well-tested mock server ready for production use.

---

## Completed Milestones

### ✅ Core Functionality
- [x] Single GET endpoint (`/api/mock`) with `X-Mockery-Mock` header
- [x] Mock file retrieval from file system
- [x] Content-Type detection from file extension
- [x] Random selection from multiple mock IDs
- [x] Custom headers via `.headers.json` files
- [x] Status code control via `.status.json` files

### ✅ Dual Storage Modes
- [x] Local mode for development (file system)
- [x] Git mode for production (LibGit2Sharp)
- [x] Strategy pattern for repository abstraction
- [x] Background refresh service for Git mode

### ✅ Mock Management API (v3.5-3.6)
- [x] GET /api/mocks - List directory contents
- [x] POST /api/mocks - Create mock files
- [x] DELETE /api/mocks - Delete mock files
- [x] Auto-commit and push in Git mode
- [x] Empty folder cleanup on delete
- [x] Idempotency check - 409 Conflict if file exists (v3.6)
- [x] Git access token configuration documentation (v3.6)

### ✅ Observability
- [x] OpenTelemetry integration (logs, metrics, traces)
- [x] Prometheus metrics endpoint (`/metrics`)
- [x] OTLP export support
- [x] Aspire Dashboard integration

### ✅ Health & Readiness
- [x] Liveness probe (`/health/live`)
- [x] Readiness probe (`/health/ready`)
- [x] Startup probe (`/health/startup`)

### ✅ Testing
- [x] 95 comprehensive unit tests
- [x] Controller tests (MockController, MocksController)
- [x] Service tests (MockService, MocksManagementService)
- [x] Repository tests (both Git and Local)
- [x] All tests passing

### ✅ Deployment
- [x] Dockerfile for container builds
- [x] Helm chart for Kubernetes
- [x] GitHub Actions CI/CD pipeline
- [x] Docker Compose for local development
- [x] `.env.example` template for configuration

### ✅ Documentation
- [x] README.md with usage instructions
- [x] Technical design document (`docs/mockery-design.md`)
- [x] Memory bank documentation
- [x] Sample mock files included

---

## Current Work

### ✅ Recently Completed (2026-01-08)
- **Removed throttling middleware and all dependencies** (v4.0)
  - Deleted: ThrottlingMiddleware, ThrottlingService, IThrottlingService, ThrottlingOptions, ThrottlingExtensions
  - Deleted: ThrottlingMiddlewareTests, ThrottlingServiceTests
  - Deleted: observability folder (Grafana dashboards, Prometheus configs)
  - Deleted: docker-compose.observability.yml, docker-compose.observability.docker.yml
  - Updated: MockeryMetrics.cs, Program.cs, appsettings files, Helm chart configs
  - Updated: MockControllerTests.cs to remove throttling dependencies
  - Updated: docs/mockery-design.md to remove throttling references
  - All 95 tests passing
- **Simplified k6 load testing** (v4.0)
  - Deleted: k6/config.js, k6/scripts/full-suite.js, k6/scripts/mocks-list.js, k6/scripts/mock-load.js
  - Created: k6/scripts/load-test.js with simple RPS and DURATION parameters
  - Updated: k6/README.md with simplified usage instructions

### 📋 Backlog
- No active backlog items

---

## Known Issues

None currently tracked.

---

## Test Results

```
Test Run Successful.
Total tests: 95
     Passed: 95
     Failed: 0
     Skipped: 0
```

### Test Coverage by Component
| Component | Tests | Status |
|-----------|-------|--------|
| MockController | Multiple | ✅ Pass |
| MocksController | 13 | ✅ Pass |
| MockService | Multiple | ✅ Pass |
| MocksManagementService | 14 | ✅ Pass |
| ContentTypeResolver | Multiple | ✅ Pass |
| GitMockRepository | Multiple | ✅ Pass |
| LocalFileMockRepository | Multiple | ✅ Pass |
| GitRepositoryRefreshService | Multiple | ✅ Pass |

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| v4.0 | 2026-01-08 | Removed throttling middleware, simplified k6 load testing. Total tests: 95. |
| v3.9 | 2025-12-20 | Multi-architecture Docker builds (amd64/arm64). |
| v3.8 | 2025-12-18 | GET /api/mocks filters hidden files, Helm chart template fixes. |
| v3.7 | 2025-12-18 | Fixed DELETE /api/mocks Git staging. Total tests: 91. |
| v3.6 | 2025-12-18 | Added idempotency check (409 Conflict), Git token config docs. Total tests: 91. |
| v3.5 | 2025-12-17 | Added Mock Management API (GET/POST/DELETE /api/mocks). Total tests: 89. |
| v3.4 | 2025-12-14 | Docker Compose port 3000→5500, memory bank moved to `memory-bank` |
| v3.3 | 2025-12-13 | Removed rate limiting references, updated project structure |
| v3.2 | 2025-11-25 | Added OpenTelemetry, `.status.json` files, updated port to 8080 |

---

## Metrics

| Metric | Value |
|--------|-------|
| **Lines of Code** | ~2,500 (estimated) |
| **Test Count** | 95 |
| **Test Coverage** | High (all major components) |
| **Dependencies** | 12 NuGet packages |
| **Container Size** | ~100MB (estimated) |

---

## Quick Commands

### Run Locally
```bash
cd src/Mockery
dotnet run
```

### Run Tests
```bash
dotnet test
```

### Build Docker Image
```bash
docker build -t mockery:latest .
```

### Deploy to Kubernetes
```bash
helm install mockery oci://ghcr.io/busadave13/helm/mockery --namespace dev
```

### Run Load Test
```bash
# Default: 10 RPS for 30 seconds
k6 run k6/scripts/load-test.js

# Custom RPS and duration
k6 run -e RPS=100 -e DURATION=60s k6/scripts/load-test.js
```

---

## API Endpoints Summary

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/mock` | GET | Retrieve mock content |
| `/api/mocks` | GET | List directory contents |
| `/api/mocks` | POST | Create mock file (409 if exists) |
| `/api/mocks` | DELETE | Delete mock file |
| `/health/live` | GET | Liveness probe |
| `/health/ready` | GET | Readiness probe |
| `/health/startup` | GET | Startup probe |
| `/metrics` | GET | Prometheus metrics |

---

## Configuration

### Git Mode with Access Token
For Git push operations, set `MOCKERY_GIT_TOKEN`:

1. **Docker Compose `.env` file** (recommended):
   ```
   MOCKERY_GIT_TOKEN=ghp_your_github_personal_access_token
   ```

2. **Environment variable**:
   ```powershell
   [Environment]::SetEnvironmentVariable("MOCKERY_GIT_TOKEN", "ghp_...", "User")
   ```

3. **Kubernetes Secret** - see design document

---

## Session Notes

### 2026-01-08
- Removed throttling middleware and all dependencies
- Deleted: ThrottlingMiddleware, ThrottlingService, IThrottlingService, ThrottlingOptions, ThrottlingExtensions
- Deleted: observability folder, docker-compose.observability files
- Updated: MockeryMetrics.cs (removed throttling counters), Program.cs, appsettings files, Helm chart
- Simplified k6 folder to single load-test.js with RPS and DURATION parameters
- All 95 tests passing
- Updated design document to v4.0
