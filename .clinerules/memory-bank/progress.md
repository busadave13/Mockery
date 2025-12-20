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
- [x] 91 comprehensive unit tests
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
- [x] Technical design document (`.docs/mockery-design.md`) v3.6
- [x] Memory bank documentation
- [x] Sample mock files included

---

## Current Work

### ✅ Recently Completed (2025-12-20)
- **Added multi-architecture Docker builds** (v3.9 - CI/CD)
  - Added QEMU setup step for cross-platform emulation
  - Added `platforms: linux/amd64,linux/arm64` to Docker build
  - Enables Mockery to run on both Windows (amd64) and Apple Silicon Macs (arm64)
  - Fixes ImagePull errors on M1/M2/M3/M4 Macs running local Kubernetes clusters
  - Updated design document to v3.9

### ✅ Previously Completed (2025-12-18)
- **GET /api/mocks now filters hidden files/folders** (v3.8 - API)
  - Hidden files and folders (starting with `.`) are now excluded from directory listings
  - Filters out `.git`, `.gitignore`, `.env`, `.vscode`, etc.
  - Added 4 new unit tests for hidden file filtering
  - All 95 tests passing
- **Fixed serviceaccount.yaml YAML Parse Error** (v3.9 - Helm)
  - Fixed invalid template syntax: `{ { .Values.namespace } }` → `{{ .Values.namespace }}`
  - Spaces inside curly braces broke Helm template parsing
  - Helm lint and template dry-run both pass successfully
  - All 91 tests still passing
- **Helm Chart Templates Updated** (v3.8)
  - Updated `service.yaml` with proper Helm templating
  - Updated `serviceaccount.yaml` with proper Helm templating
  - Updated `httproute.yaml` with proper Helm templating and conditional rendering
  - Updated `canary.yaml` with proper Helm templating and conditional rendering
  - Added `httpRoute` and `canary` sections to `values.yaml` with full configurability
  - All templates now reference values instead of hardcoded values
  - Helm lint passed successfully
  - All 91 tests still passing
- Fixed DELETE /api/mocks Git staging - delete operations now use `Commands.Remove()` instead of `Commands.Stage()` since the file no longer exists after deletion (v3.7)
- Fixed POST /api/mocks Git commit/push not working (staging with explicit file path instead of wildcard)
- Added idempotency check - POST /api/mocks now returns 409 Conflict if file already exists
- Added 2 new unit tests for idempotency behavior
- Added Git access token configuration documentation
- Created `.env.example` template file
- Updated design document to v3.7
- Total tests: 91

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
| v3.7 | 2025-12-18 | Fixed DELETE /api/mocks Git staging - uses `Commands.Remove()` for deletions. Total tests: 91. |
| v3.6 | 2025-12-18 | Added idempotency check (409 Conflict), Git token config docs. Total tests: 91. |
| v3.5 | 2025-12-17 | Added Mock Management API (GET/POST/DELETE /api/mocks). Total tests: 89. |
| v3.4 | 2025-12-14 | Docker Compose port 3000→5500, memory bank moved to `.clinerules/memory-bank` |
| v3.3 | 2025-12-13 | Removed rate limiting references, updated project structure |
| v3.2 | 2025-11-25 | Added OpenTelemetry, `.status.json` files, updated port to 8080 |
| v3.1 | 2025-11-25 | Documentation updates |
| v3.0 | 2025-11-16 | Dual-mode repository support, Strategy pattern |

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

### 2025-12-18 (Session 2)
- Fixed DELETE /api/mocks Git staging - delete operations now use `Commands.Remove()` instead of `Commands.Stage()`
- Root cause: After `base.DeleteFileAsync()` deletes the file, `Commands.Stage()` fails because the file no longer exists
- Solution: Use `Commands.Remove(repo, normalizedPath, removeFromWorkingDirectory: false)` for delete operations
- Updated design document to v3.7
- All 91 tests passing

### 2025-12-18 (Session 1)
- Fixed POST /api/mocks Git commit/push - was using wildcard path instead of specific file path
- Added idempotency check - returns 409 Conflict if file already exists
- Added `.env.example` template for token configuration
- Updated design document to v3.6 with:
  - 409 Conflict response documentation
  - Git access token configuration section
  - Updated version history
- All 91 tests passing

### 2025-12-17
- Implemented Mock Management API with 3 new endpoints
- Created MocksController, MocksManagementService, and response models
- Added Git commit/push functionality for create and delete operations
- Created 23 new unit tests (11 for controller, 12 for service)
- Updated design document to v3.5
- Updated memory bank documentation

### 2025-12-14 (Session 2)
- Updated Docker Compose port mapping from 3000 to 5500
- Moved memory bank from `.memory-bank` to `.clinerules/memory-bank`
- Updated README.md with new port 5500
- Updated design document to v3.4
- Created GitHub issue #51

### 2025-12-14 (Session 1)
- Initialized memory bank with 5 core documents
