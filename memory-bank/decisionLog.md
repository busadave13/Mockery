# Decision Log - Mockery

This document captures key architectural and technical decisions made during the development of Mockery.

---

## Decision 1: Git Repository Over Database

**Date:** Project inception  
**Status:** ✅ Implemented  
**Context:** Need to store and manage mock files for HTTP responses.

### Decision
Use Git repository for storing mock files instead of MongoDB or other database.

### Rationale
- **Version Control**: Full audit trail of all mock changes via Git history
- **Collaboration**: Standard pull request workflows for reviewing mock changes
- **Simplicity**: No database infrastructure, connection pooling, or schema management
- **Transparency**: Mock content visible in standard text editors and Git tools
- **Branching**: Support for feature branches, staging environments via Git branches
- **Rollback**: Easy rollback to previous mock versions via Git revert/reset
- **Developer Familiarity**: Teams already use Git daily

### Trade-offs
- **Query Performance**: No indexed lookups (mitigated by file system caching)
- **Concurrency**: File system locks instead of database transactions (acceptable for read-only)
- **Scalability**: Limited by file system performance (acceptable for testing use case)

### Implementation
- LibGit2Sharp 0.30.0 for Git operations in C#
- Clone repository on service startup
- Periodic refresh to pull latest changes (configurable interval)

---

## Decision 2: Status Files for HTTP Status Codes

**Date:** v3.2 (2025-11-25)  
**Status:** ✅ Implemented  
**Context:** Previous approach used `X-Mock-StatusCode` request header.

### Decision
Use `.status.json` files with status code in filename instead of request headers.

### Rationale
- **File-Based**: Keeps all mock configuration in files (no special headers)
- **Version Controlled**: Status responses versioned alongside regular mocks
- **Reusable**: Same status file can be used across test runs
- **Self-Documenting**: Filename clearly indicates expected status code
- **Simple Client**: Clients just specify mock ID, no extra headers needed

### Trade-offs
- **Multiple Files**: Need separate file per status code
- **Naming Convention**: Status code must be first part of filename
- **Validation**: Status code extracted from filename must be valid (100-599)

### Implementation
```
mocks/FooBar/504.status.json → Returns HTTP 504
mocks/FooBar/200.status.json → Returns HTTP 200
```

---

## Decision 3: File Extension for Content-Type Detection

**Date:** Project inception  
**Status:** ✅ Implemented  
**Context:** Need to determine HTTP Content-Type header for responses.

### Decision
Determine HTTP Content-Type header from file extension instead of metadata files or configuration.

### Rationale
- **Simplicity**: No separate configuration files or database records
- **Clarity**: Extension immediately indicates response format
- **Standard Practice**: Follows web server conventions (Apache, Nginx)
- **Tooling Support**: Editors provide syntax highlighting based on extension

### Supported Extensions
| Extension | Content-Type |
|-----------|--------------|
| `.json` | `application/json` |
| `.html` | `text/html` |
| `.xml` | `application/xml` |
| `.txt` | `text/plain` |
| `.css` | `text/css` |
| `.js` | `application/javascript` |
| (default) | `application/octet-stream` |

---

## Decision 4: Optional Headers Files

**Date:** Project inception  
**Status:** ✅ Implemented  
**Context:** Need to support custom HTTP response headers for some mocks.

### Decision
Support optional `.headers.json` files alongside mock files for custom HTTP response headers.

### Rationale
- **Flexibility**: Enable testing of custom headers (authentication, caching, etc.)
- **Simplicity Preserved**: Headers files are optional; simple mocks work without them
- **Separation of Concerns**: Response content in mock file, custom headers in separate file
- **Real-World Testing**: Simulate various HTTP headers for comprehensive testing
- **Backward Compatible**: Existing mocks without headers files work with Content-Type only

### Trade-offs
- **Additional Files**: Requires two files for mocks with custom headers
- **Consistency**: Must keep mock and headers files in sync
- **Naming Convention**: Developers must follow `.headers.json` convention

---

## Decision 5: Dual Storage Modes (Local + Git)

**Date:** v3.0 (2025-11-16)  
**Status:** ✅ Implemented  
**Context:** Development workflow needed simpler setup than Git mode.

### Decision
Implement Strategy pattern to support both Local file system mode and Git repository mode.

### Rationale
- **Development Experience**: No Git setup required for local development
- **Instant Feedback**: File changes picked up immediately in local mode
- **Production Ready**: Git mode provides full version control for production
- **Same Code Path**: Both modes use shared file operations

### Implementation
```csharp
IGitMockRepository (interface)
       ↑
FileSystemMockRepositoryBase (abstract)
       ↑
   ┌───┴───┐
GitMockRepository   LocalFileMockRepository
```

---

## Decision 6: No Authentication

**Date:** Project inception  
**Status:** ✅ Implemented  
**Context:** Determine security model for the service.

### Decision
Remove all authentication and authorization mechanisms.

### Rationale
- **Simplicity**: No user management, API keys, or JWT validation
- **Testing Focus**: Service intended for development/testing environments
- **Network Security**: Infrastructure-level security sufficient
- **Reduced Dependencies**: No Firebase, no user database

### Security Model
- **Network-Level**: Deploy in private networks or behind VPN
- **Infrastructure-Level**: Use Azure Front Door, API Gateway, or firewall rules

---

## Decision 7: OpenTelemetry for Observability

**Date:** v3.2 (2025-11-25)  
**Status:** ✅ Implemented  
**Context:** Need observability for monitoring and debugging.

### Decision
Full OpenTelemetry integration for logging, metrics, and tracing.

### Rationale
- **Standard Protocol**: OTEL is industry standard for observability
- **Flexible Export**: Support Prometheus, OTLP, Console exporters
- **Comprehensive**: Metrics, traces, and logs in one framework
- **Cloud Native**: Works with Aspire, Jaeger, Zipkin, etc.

### Implementation
- Prometheus scraping endpoint at `/metrics`
- OTLP export when `OTEL_EXPORTER_OTLP_ENDPOINT` configured
- Console fallback for local development
- ASP.NET Core and HTTP client instrumentation

---

## Decision 8: Service Folder Organization

**Date:** Project inception  
**Status:** ✅ Implemented  
**Context:** Need to organize mocks efficiently.

### Decision
Organize mocks by service folders and require service name prefix in mock IDs.

### Rationale
- **Explicit Addressing**: Mock ID includes service name (e.g., `FooBar/1234`)
- **Direct Lookup**: Direct path resolution, no cross-folder searching
- **Performance**: O(1) file lookup per service folder
- **Clarity**: Clear separation of mocks by service
- **Scalability**: Avoid thousands of files in single directory
- **Collision Prevention**: Same file ID can exist in different services

---

## Decisions Under Consideration

### Potential: In-Memory Caching
- **Status**: 🔄 Under consideration
- **Rationale**: Could improve performance for frequently accessed mocks
- **Trade-off**: Adds complexity, may not be needed given OS file caching

### Potential: Webhook-Triggered Git Refresh
- **Status**: 🔄 Under consideration
- **Rationale**: Real-time updates instead of polling
- **Trade-off**: Requires webhook configuration on Git repository
