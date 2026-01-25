# Mockery - Technical Design Document

**Version:** 5.0
**Date:** 2026-01-24
**Author:** System Architecture Team
**Status:** Living Document

> **⚠️ MAJOR REFACTOR (v5.0):** This version removes the LibGit2Sharp dependency and introduces Kubernetes-native Git operations using init containers and git-sync sidecars. Production mode is now **read-only** with multi-repository support. See [Section 6.1](#61-kubernetes-native-git-operations) for details.

---

## Table of Contents

1. [Overview & Context](#1-overview--context)
2. [Goals & Non-Goals](#2-goals--non-goals)
3. [System Architecture](#3-system-architecture)
4. [Data Model](#4-data-model)
5. [API Design](#5-api-design)
6. [Key Technical Decisions](#6-key-technical-decisions)
7. [Dependencies](#7-dependencies)
8. [Architecture Diagrams](#8-architecture-diagrams)
9. [Cross-Cutting Concerns](#9-cross-cutting-concerns)
10. [Testing Strategy](#10-testing-strategy)
11. [CI/CD & Deployment](#11-cicd--deployment)
12. [Development Workflow](#12-development-workflow)
13. [Future Considerations](#13-future-considerations)
14. [References](#14-references)

---

## 1. Overview & Context

### 1.1 Problem Statement

Development and testing teams need a simple, reliable way to serve mock HTTP responses for:
- Testing microservices in isolation
- Simulating third-party API responses
- Creating predictable test environments
- Supporting contract testing across distributed systems

Mockery provides a flexible mock server with dual storage modes:

**Development Mode (Local File System):**
- Reads and writes mocks directly from local file system (`./mocks/` directory)
- Zero setup - no Git configuration required
- Instant mock changes without restart
- Full CRUD support (GET, POST, DELETE)
- Single repository only (`X-Mockery-Repo` header not supported)
- Perfect for rapid local development and testing

**Production Mode (Kubernetes with Git Sync):**
- **Read-only** - mocks served from Git repositories synced via Kubernetes-native tooling
- **Multi-repository support** - configure multiple Git repos via ConfigMap
- **Init container** clones repositories on pod startup
- **git-sync sidecar** periodically pulls latest changes from remote
- **No LibGit2Sharp** - eliminates native library dependencies
- POST and DELETE endpoints return `501 Not Implemented`
- Repository selection via `X-Mockery-Repo` header (optional, defaults to configured default)

**Common Features:**
- Single HTTP endpoint for mock retrieval
- File-based organization by service name
- Status code control via `.status.json` files
- Custom headers via `.headers.json` files
- Full OpenTelemetry observability (metrics, traces, logs)
- No databases, authentication, or complex infrastructure required

### 1.2 Business Impact

- **Developer Productivity:** Mocks managed via Git commits, pull requests, and standard development workflows
- **Simplicity:** Single API endpoint reduces cognitive overhead
- **Version Control:** Full audit trail of mock changes via Git history
- **Collaboration:** Standard Git workflows for reviewing and approving mock changes
- **Cost Efficiency:** No database infrastructure or authentication services required
- **Observability:** Built-in OpenTelemetry integration for monitoring and debugging

### 1.3 Stakeholders

- **Development Teams:** Primary users consuming mocks for testing
- **QA Engineers:** Using mocks for integration and E2E testing
- **DevOps Engineers:** Managing deployment and infrastructure
- **Technical Leads:** Reviewing and approving mock changes via Git workflows

---

## 2. Goals & Non-Goals

### 2.1 Goals

1. **Simple Mock Retrieval**
   - Single GET endpoint with header-based mock ID selection
   - Support single mock ID or random selection from multiple IDs
   - Return raw mock content with appropriate Content-Type

2. **Dual-Mode Storage**
   - **Local Mode (Development):** File system-based storage from `./mocks/` directory with full read/write
   - **Production Mode (Kubernetes):** Read-only access to Git-synced repositories
   - **Multi-Repository Support (Production):** Configure multiple Git repositories via Kubernetes ConfigMap
   - **Repository Selection:** Optional `X-Mockery-Repo` header to select repository (defaults to configured default)
   - Organize mocks by service folders in both modes
   - File extension determines Content-Type (.json, .html, etc.)
   - Strategy pattern allows easy switching between storage backends

3. **Status Code Control**
   - Status codes via `.status.json` files (e.g., `504.status.json` returns HTTP 504)
   - Status code extracted from filename (numeric portion)
   - Optional content in status files for error response bodies

4. **Custom Headers Support**
   - Optional `.headers.json` files for custom HTTP response headers
   - Headers merged with auto-detected Content-Type
   - Simple key-value JSON structure

5. **Developer Experience**
   - Standard Git workflows for managing mocks
   - Pull request reviews for mock changes
   - Clear file organization by service
   - Swagger UI in development mode

6. **Content-Type Support**
   - Automatic Content-Type detection from file extension
   - Support common formats (JSON, HTML, XML, plain text, images, etc.)

7. **Observability**
   - Full OpenTelemetry integration (metrics, traces, logs)
   - Prometheus metrics endpoint for scraping
   - OTLP export support for centralized telemetry
   - Console fallback when OTLP endpoint not configured

### 2.2 Non-Goals

1. **Authentication:** No user authentication or authorization
2. **User Management:** No user profiles, API keys, or account management
3. **Statistics:** No request counting or usage analytics
4. **Write Operations in Production:** POST and DELETE return `501 Not Implemented` in production (Kubernetes) mode
5. **Environment Routing:** No environment-specific mock selection
6. **Probe Tracking:** No client application monitoring
7. **Complex Request Matching:** No endpoint, method, or query parameter matching
8. **Response Templating:** No dynamic response generation
9. **Rate Limiting:** No rate limiting or throttling middleware
10. **Multi-Repository in Development:** `X-Mockery-Repo` header not supported in local development mode

---

## 3. System Architecture

### 3.1 High-Level Architecture

Mockery follows a three-layer architecture pattern with pluggable storage:

**Presentation Layer:**
- ASP.NET Core 9.0 REST API with single GET endpoint
- OpenTelemetry observability middleware
- CORS configuration for cross-origin requests
- Health check endpoints for orchestration
- Swagger/OpenAPI documentation (development mode)

**Business Logic Layer:**
- Service layer for mock ID parsing and status code semantics
- Random selection logic for multiple mock IDs
- Content-type resolution
- Repository routing based on `X-Mockery-Repo` header (production mode)

**Repository Layer (Strategy Pattern):**
- Abstract base class `FileSystemMockRepositoryBase` with shared file operations
- **Local Mode:** `LocalFileMockRepository` for direct file system access (full read/write)
- **Production Mode:** `MultiRepoFileMockRepository` for read-only access to multiple git-synced paths
- Repository implementation selected at startup based on configuration

**Kubernetes Infrastructure (Production Only):**
- **Init Container:** Clones Git repositories on pod startup
- **git-sync Sidecar:** Periodically pulls latest changes from remote repositories
- **ConfigMap:** Multi-repository configuration with URLs, branches, and mount paths
- **Secret:** Personal Access Tokens (PATs) for Git authentication

### 3.2 Core Components

#### 3.2.1 Controller (`src/Mockery/Controllers/`)

| Controller | Route | Authorization | Purpose |
|------------|-------|---------------|---------|
| `MockController` | `/api/mock` | None | Retrieve mock content by mock ID(s) |
| `MocksController` | `/api/mocks` | None | Mock Management API (list, create, delete) |

**MockController - HTTP Layer Responsibilities:**
- Extract `X-Mockery-Mock` header from HTTP request
- Parse comma-separated mock IDs into collection (e.g., `"FooBar/1234,FooBar/5678"` → `["FooBar/1234", "FooBar/5678"]`)
- Validate header presence and format
- Call business logic service with parsed mock IDs
- Receive domain result from business logic (content, content-type, headers, status code from `.status.json` file)
- Set HTTP response headers (Content-Type, custom headers)
- Set HTTP response status code (from `.status.json` file or default 200 OK)
- Return file contents as HTTP response body (if applicable based on status code)
- Handle exceptions and return appropriate HTTP status codes (400, 404, 500)

**MocksController - HTTP Layer Responsibilities:**
- **GET /api/mocks:** List directory contents at specified path (X-Mockery-Mock header optional)
- **POST /api/mocks:** Create new mock file (development mode only; returns `501` in production)
- **DELETE /api/mocks:** Delete mock file (development mode only; returns `501` in production)
- Validate header presence and format for POST/DELETE
- Return appropriate HTTP status codes (200, 201, 400, 404, 409, 501)
- Delegates to `IMocksManagementService` for all operations

**Separation of Concerns:**
- **Does:** Parse HTTP headers, set HTTP responses, handle HTTP status codes
- **Does NOT:** Contain business logic, perform random selection, access repository directly
- **Delegates to:** `IMockService` for mock retrieval, `IMocksManagementService` for mock management

#### 3.2.2 Business Logic (`src/Mockery/BusinessLogic/`)

**Interface:** `IMockService`

```csharp
public interface IMockService
{
    Task<MockFileResult?> GetMockAsync(IEnumerable<string> mockIds);
}
```

**Implementation:** `MockService`

**Key Responsibilities:**
- Accept parsed mock IDs (including service name) as parameters (no HTTP context access)
- Random selection for multiple mock IDs using `Random.Shared`
- Parse mock ID to extract service name and file ID (e.g., `FooBar/1234` → service: `FooBar`, fileId: `1234`)
- Check for `.status.json` file first (`{ServiceName}/{FileId}.status.json`)
- Parse status code from filename (e.g., `504` from `504.status.json`)
- Check for optional headers file (`{ServiceName}/{FileId}.headers.json`)
- Parse headers file to extract custom HTTP headers (if present)
- Locate regular mock file if no status file found
- Coordinate file retrieval and content-type resolution
- Return appropriate response based on file type:
  - **`.status.json` file found:** Return content with HTTP status from filename
  - **Regular mock file found:** Return content with HTTP 200 OK
  - **No file found:** Return null (controller handles 404)
- Handle file not found scenarios

**Interface:** `IMocksManagementService`

```csharp
public interface IMocksManagementService
{
    Task<DirectoryListingResponse> ListDirectoryAsync(string path);
    Task<CreateMockResponse> CreateFileAsync(string path, string content);
    Task<DeleteMockResponse> DeleteFileAsync(string path);
}
```

**Implementation:** `MocksManagementService`

**Key Responsibilities:**
- List directory contents within mocks folder
- Create new mock files with validation
- Delete mock files and clean up empty parent folders
- Delegates Git/file operations to repository layer

**Status Code Behavior Logic:**
- **`.status.json` file found:** Extract status code from filename (e.g., `504.status.json` → 504)
- **Status file content:** Return file content as response body (can be empty)
- **Status code 204:** No content returned (by HTTP specification)
- **Regular mock file found:** Return content with HTTP 200 OK
- **No file found:** Return null (controller handles 404)

**Separation of Concerns:**
- **Does NOT** parse HTTP headers or access HttpContext
- **Does NOT** interact with HTTP request/response objects
- **Receives** already-parsed mock IDs from controller layer
- **Returns** domain objects (`MockFileResult`) not HTTP responses

#### 3.2.3 Models (`src/Mockery/Models/`)

**MockFileResult:**
```csharp
public class MockFileResult
{
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
    public bool ShouldReturnContent { get; set; } = true;
    
    /// <summary>
    /// Status code derived from a .status.json file (e.g., 504 from 504.status.json)
    /// </summary>
    public int? StatusCode { get; set; }
}
```

**DirectoryListingResponse:**
```csharp
public record DirectoryListingResponse
{
    public string Path { get; init; } = string.Empty;
    public List<DirectoryItem> Items { get; init; } = new();
}

public record DirectoryItem
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;  // "folder" or "file"
    public string? Extension { get; init; }
    public long? Size { get; init; }
}
```

**CreateMockResponse:**
```csharp
public record CreateMockResponse
{
    public string Path { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long Size { get; init; }
    public bool CommittedToGit { get; init; }
}
```

**DeleteMockResponse:**
```csharp
public record DeleteMockResponse
{
    public string DeletedFile { get; init; } = string.Empty;
    public List<string> DeletedFolders { get; init; } = new();
    public bool CommittedToGit { get; init; }
}
```

#### 3.2.4 Repository Layer (`src/Mockery/Repository/`)

**Interface:** `IMockRepository`

```csharp
public interface IMockRepository
{
    Task InitializeAsync();
    Task<(string Content, string Extension)?> FindMockFileAsync(string serviceName, string fileId);
    Task<Dictionary<string, string>?> FindHeadersFileAsync(string serviceName, string fileId);
    Task<(int StatusCode, string? Content)?> FindStatusFileAsync(string serviceName, string fileId);
    
    // Management API methods
    Task<DirectoryListingResponse> ListDirectoryAsync(string path);
    Task<CreateMockResponse> CreateFileAsync(string path, string content);
    Task<DeleteMockResponse> DeleteFileAsync(string path);
    
    bool IsReadOnly { get; }
}
```

**Base Class:** `FileSystemMockRepositoryBase` (abstract)

**Implementations:**
1. `LocalFileMockRepository` (Development mode - full read/write)
2. `MultiRepoFileMockRepository` (Production mode - read-only, multi-repo)

**Architecture (Strategy Pattern):**
```
IMockRepository (interface)
       ↑
       |
FileSystemMockRepositoryBase (abstract base)
       ↑
       |
   ┌───┴────┐
   |        |
LocalFile   MultiRepo
MockRepo    FileMockRepo
(Dev)       (Prod)
```

**Shared Responsibilities (FileSystemMockRepositoryBase):**
- Locate status files using direct path: `mocks/{ServiceName}/{FileId}.status.json`
- Parse status code from fileId (must be valid HTTP status code 100-599)
- Locate mock files using pattern: `mocks/{ServiceName}/{FileId}.*` (excluding .headers.json and .status.json)
- Locate optional headers files using direct path: `mocks/{ServiceName}/{FileId}.headers.json`
- Read file contents from file system
- Parse headers JSON files using `System.Text.Json`
- Support file extension detection (search for `{ServiceName}/{FileId}.*` to find extension)
- Thread-safe operations via `SemaphoreSlim`
- List directory contents (filters out hidden files starting with `.`)

**LocalFileMockRepository-Specific Responsibilities (Development):**
- Verify local mocks directory exists
- Create mocks directory if missing (`{LocalPath}/mocks`)
- Full read/write support (POST and DELETE operations)
- Single repository only (`X-Mockery-Repo` header returns `400 Bad Request`)
- `IsReadOnly` returns `false`

**MultiRepoFileMockRepository-Specific Responsibilities (Production):**
- Manage multiple repository mount paths from configuration
- Resolve repository by name from `X-Mockery-Repo` header
- Fall back to `defaultRepository` when header not provided
- Return `404` if requested repository name not found in configuration
- **Read-only:** `CreateFileAsync` and `DeleteFileAsync` throw `NotSupportedException`
- `IsReadOnly` returns `true`
- No Git operations - relies on init container and git-sync sidecar

**Removed Components (v5.0):**
- ~~`GitMockRepository`~~ - Replaced by `MultiRepoFileMockRepository` + Kubernetes git-sync
- ~~`IGitMockRepository`~~ - Replaced by `IMockRepository`
- ~~`GitRepositoryOptions`~~ - Replaced by `MultiRepoSettings`
- ~~`GitRepositoryRefreshService`~~ - Replaced by git-sync sidecar
- ~~`LibGit2Sharp` NuGet package~~ - No longer required

**Configuration-Based Selection:**
Repository implementation is selected at startup based on `appsettings.json`:
```json
{
  "MockRepository": {
    "Type": "Local",        // or "MultiRepo"
    "LocalPath": "./mocks"  // for Local mode
  }
}
```

#### 3.2.5 Content-Type Resolver (`src/Mockery/Services/`)

**Interface:** `IContentTypeResolver`

**Class:** `ContentTypeResolver`

**Responsibilities:**
- Map file extensions to MIME types
- Support common formats:
  - `.json` → `application/json`
  - `.html` → `text/html`
  - `.xml` → `application/xml`
  - `.txt` → `text/plain`
  - `.csv` → `text/csv`
  - `.pdf` → `application/pdf`
  - `.js` → `application/javascript`
  - `.css` → `text/css`
  - `.png` → `image/png`
  - `.jpg` / `.jpeg` → `image/jpeg`
  - `.gif` → `image/gif`
  - `.svg` → `image/svg+xml`
  - Default → `application/octet-stream`

#### 3.2.6 Kubernetes Components (Production Mode Only)

Production deployments use Kubernetes-native tooling for Git operations instead of in-process LibGit2Sharp.

##### 3.2.6.1 Init Container

**Purpose:** Clone Git repositories on pod startup before main container starts.

**Image:** `alpine/git:latest` (or similar lightweight git image)

**Behavior:**
1. Read repository configuration from mounted ConfigMap
2. For each configured repository:
   - Check if `mountPath` directory exists
   - If directory **does not exist**: clone repository using PAT from Secret
   - If directory **exists**: skip (already cloned, sidecar will sync)
3. Exit with code `0` only when all clones complete successfully
4. Main container startup blocked until init container exits

**Configuration (from ConfigMap):**
```yaml
defaultRepository: "primary"
repositories:
  - name: "primary"
    url: "https://github.com/org/mocks-primary.git"
    branch: "main"
    mountPath: "/app/mocks/primary"
    secretKeyRef: "primary-pat"
  - name: "secondary"
    url: "https://github.com/org/mocks-secondary.git"
    branch: "main"
    mountPath: "/app/mocks/secondary"
    secretKeyRef: "secondary-pat"
```

**Init Container Script (conceptual):**
```bash
#!/bin/sh
set -e

# Parse ConfigMap JSON/YAML for repositories
for repo in $(cat /config/repositories.json | jq -r '.repositories[] | @base64'); do
    name=$(echo $repo | base64 -d | jq -r '.name')
    url=$(echo $repo | base64 -d | jq -r '.url')
    branch=$(echo $repo | base64 -d | jq -r '.branch')
    mountPath=$(echo $repo | base64 -d | jq -r '.mountPath')
    secretKey=$(echo $repo | base64 -d | jq -r '.secretKeyRef')
    
    if [ ! -d "$mountPath/.git" ]; then
        echo "Cloning $name to $mountPath..."
        pat=$(cat /secrets/$secretKey)
        git clone --branch $branch --single-branch \
            "https://${pat}@${url#https://}" "$mountPath"
    else
        echo "$name already exists at $mountPath, skipping clone"
    fi
done

echo "All repositories cloned successfully"
```

##### 3.2.6.2 git-sync Sidecar

**Purpose:** Periodically pull latest changes from remote Git repositories.

**Image:** `registry.k8s.io/git-sync/git-sync:v4.4.0`

**Behavior:**
- Runs alongside main container in same pod
- Shares volume mounts with main container
- Pulls changes on configurable interval (default: 60 seconds)
- One sidecar container per repository
- Pull-only operation (no push support)

**Key Configuration Options:**
| Environment Variable | Description | Example |
|---------------------|-------------|---------|
| `GITSYNC_REPO` | Git repository URL | `https://github.com/org/mocks.git` |
| `GITSYNC_BRANCH` | Branch to sync | `main` |
| `GITSYNC_ROOT` | Local directory to sync to | `/app/mocks/primary` |
| `GITSYNC_PERIOD` | Sync interval | `60s` |
| `GITSYNC_USERNAME` | Git username (for PAT auth) | `oauth2` |
| `GITSYNC_PASSWORD_FILE` | Path to file containing PAT | `/secrets/primary-pat` |
| `GITSYNC_ONE_TIME` | Exit after first sync | `false` |
| `GITSYNC_DEPTH` | Git clone depth (shallow clone) | `1` |

**Example Sidecar Configuration:**
```yaml
- name: git-sync-primary
  image: registry.k8s.io/git-sync/git-sync:v4.4.0
  env:
    - name: GITSYNC_REPO
      value: "https://github.com/org/mocks-primary.git"
    - name: GITSYNC_BRANCH
      value: "main"
    - name: GITSYNC_ROOT
      value: "/app/mocks/primary"
    - name: GITSYNC_PERIOD
      value: "60s"
    - name: GITSYNC_USERNAME
      value: "oauth2"
    - name: GITSYNC_PASSWORD_FILE
      value: "/secrets/primary-pat"
  volumeMounts:
    - name: mocks-primary
      mountPath: /app/mocks/primary
    - name: git-secrets
      mountPath: /secrets
      readOnly: true
```

##### 3.2.6.3 Multi-Repository ConfigMap Schema

**ConfigMap:** `mockery-repos-config`

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: mockery-repos-config
data:
  repositories.json: |
    {
      "defaultRepository": "primary",
      "repositories": [
        {
          "name": "primary",
          "url": "https://github.com/org/mocks-primary.git",
          "branch": "main",
          "mountPath": "/app/mocks/primary",
          "secretKeyRef": "primary-pat"
        },
        {
          "name": "secondary",
          "url": "https://github.com/org/mocks-secondary.git",
          "branch": "develop",
          "mountPath": "/app/mocks/secondary",
          "secretKeyRef": "secondary-pat"
        }
      ]
    }
```

**Field Descriptions:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `defaultRepository` | string | Yes | Name of repository to use when `X-Mockery-Repo` header is not provided |
| `repositories` | array | Yes | List of repository configurations |
| `repositories[].name` | string | Yes | Unique identifier for the repository (used in `X-Mockery-Repo` header) |
| `repositories[].url` | string | Yes | Git repository URL (HTTPS) |
| `repositories[].branch` | string | Yes | Branch to clone/sync |
| `repositories[].mountPath` | string | Yes | Absolute path where repository will be mounted |
| `repositories[].secretKeyRef` | string | Yes | Key name in Kubernetes Secret containing the PAT |

##### 3.2.6.4 Kubernetes Secret Schema

**Secret:** `mockery-git-secrets`

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: mockery-git-secrets
type: Opaque
stringData:
  primary-pat: ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
  secondary-pat: ghp_yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy
```

**Notes:**
- Each key in the Secret corresponds to a `secretKeyRef` in the ConfigMap
- PATs require `repo` scope for private repositories or `public_repo` for public repositories
- Use `stringData` for plain text values or `data` for base64-encoded values

#### 3.2.7 OpenTelemetry Extensions (`src/Mockery/Extensions/`)

**Class:** `OpenTelemetryExtensions`

**Responsibilities:**
- Configure OpenTelemetry for logging, metrics, and tracing
- Support standard OTEL environment variables
- Configure Prometheus metrics endpoint
- OTLP export for centralized telemetry
- Fallback to console exporter when OTLP not configured

**Environment Variables:**
- `OTEL_SERVICE_NAME`: Service name for telemetry
- `OTEL_EXPORTER_OTLP_ENDPOINT`: OTLP endpoint URL
- `OTEL_EXPORTER_OTLP_PROTOCOL`: Protocol (grpc or http/protobuf)
- `OTEL_EXPORTER_OTLP_HEADERS`: Optional headers for authentication

**Metrics Instrumentation:**
- ASP.NET Core hosting metrics
- Kestrel server metrics
- HTTP client metrics
- System.Net metrics

**Extension Methods:**
- `AddObservability(this WebApplicationBuilder)`: Configure OTEL services
- `UseObservability(this WebApplication)`: Configure Prometheus endpoint

---

## 4. Data Model

### 4.1 Storage Structure

**Repository Organization:**
```
mocks/
├── FooBar/
│   ├── 1234.json              # Regular mock file
│   ├── 1234.headers.json      # Optional headers for 1234
│   ├── 200.status.json        # Status file - returns HTTP 200
│   ├── 504.status.json        # Status file - returns HTTP 504
│   └── 5678.html              # HTML mock file
├── BarBaz/
│   ├── 789.json
│   └── 101.txt
└── Products/
    ├── hydrate.json
    └── error.json
```

**File Types:**

| File Pattern | Purpose | Required | Example |
|--------------|---------|----------|---------|
| `{id}.{ext}` | Response body content | Yes (or status file) | `1234.json`, `user.html` |
| `{id}.headers.json` | Custom HTTP headers | No | `1234.headers.json` |
| `{statusCode}.status.json` | HTTP status code + optional body | No | `404.status.json`, `500.status.json` |

**File Naming Convention:**
- Mock file format: `{ServiceName}/{MockId}.{extension}`
- Headers file format (optional): `{ServiceName}/{MockId}.headers.json`
- Status file format: `{ServiceName}/{StatusCode}.status.json`
- Mock ID format: `{ServiceName}/{MockId}` (e.g., `FooBar/1234`, `Products/hydrate`)
- Service name: Must match service folder name (case-sensitive)
- File ID: Numeric or alphanumeric identifier within service
- Extension: Determines Content-Type (`.json`, `.html`, `.xml`, `.txt`, etc.)

### 4.2 File Organization

**Service Folder:**
- Represents a logical service or API
- Contains all mocks for that service
- Naming convention: PascalCase (e.g., `UserService`, `PaymentGateway`)

**Mock Files:**
- Each file contains the complete HTTP response body
- File extension determines Content-Type header
- Optional headers files for custom response headers

**Status Files:**
- Filename must be valid HTTP status code (100-599)
- Extension must be `.status.json`
- Content is optional (can be empty file)
- When requested, returns the status code from filename

**Headers Files (Optional):**
- Naming: `{ServiceName}/{MockId}.headers.json`
- Contains custom HTTP response headers as key-value pairs
- If not present, response includes only Content-Type header (from file extension)
- Provides flexibility for custom headers without sacrificing simplicity

### 4.3 Example Mock Files

**FooBar/1234.json:**
```json
{
    "id": 1234,
    "name": "Sample Item",
    "status": "active"
}
```

**FooBar/1234.headers.json:**
```json
{
    "X-Custom-Header": "CustomValue",
    "X-Request-ID": "abc-123-def-456",
    "Cache-Control": "no-cache"
}
```

**FooBar/504.status.json:**
```json
{
    "error": "Gateway Timeout",
    "message": "The upstream server did not respond in time"
}
```

**FooBar/5678.html:**
```html
<!DOCTYPE html>
<html>
<head>
    <title>Mock Response</title>
</head>
<body>
    <h1>Mock HTML Response</h1>
    <p>This is a test response.</p>
</body>
</html>
```

### 4.4 Status File Resolution

When you request `X-Mockery-Mock: FooBar/504`:

1. **Status file first:** `mocks/FooBar/504.status.json` → Returns with HTTP 504
2. **Content file second:** `mocks/FooBar/504.json` → Returns with HTTP 200
3. **Not found:** Returns HTTP 404

**Status Code Priority:**
1. `.status.json` file - Status code from the filename
2. Default 200 OK - When no status file exists

### 4.5 Data Flow Contracts

**Mock Request Headers:**
```
X-Mockery-Mock: FooBar/1234
```
or (multiple mock IDs for random selection):
```
X-Mockery-Mock: FooBar/1234,FooBar/5678,Products/hydrate
```
or (status file for error response):
```
X-Mockery-Mock: FooBar/504
```

**Mock Response:**
- HTTP Status: From `.status.json` filename (or default 200 OK, or 404 if mock not found)
- Content-Type: Determined from file extension (if content returned)
- Custom Headers: From `.headers.json` file (if exists)
- Body: Raw file contents (unless status code is 204)

---

## 5. API Design

### 5.1 Authentication

**No Authentication Required:** All endpoints are publicly accessible.

**Security Considerations:**
- Service intended for development/testing environments
- Production deployment should use network-level security (VPN, private networks)
- Optional: Add IP whitelisting at infrastructure level

### 5.2 Endpoint Specifications

#### 5.2.1 Mock Retrieval API

**GET /api/mock**
- **Auth:** None
- **Headers:**
  - `X-Mockery-Mock: <service>/<mock-id>` or `X-Mockery-Mock: <service1>/<id1>,<service2>/<id2>` (required)
  - `X-Mockery-Repo: <repository-name>` (optional, production mode only)
- **Response:** HTTP status code (from `.status.json` or default 200) with mock file contents and custom headers
- **Content-Type:** Determined from file extension
- **Behavior:**
  - Parse `X-Mockery-Mock` header (required, format: `ServiceName/MockId`)
  - **Production mode:** Parse `X-Mockery-Repo` header to select repository
    - If header present: Use specified repository name
    - If header absent: Use `defaultRepository` from configuration
    - If repository name not found: Return `404 Not Found`
  - **Development mode:** `X-Mockery-Repo` header returns `400 Bad Request` (not supported)
  - If single mock ID: Use that mock ID
  - If multiple mock IDs (comma-separated): Randomly select one using `Random.Shared`
  - Check for status file first (`{ServiceName}/{MockId}.status.json`)
  - If status file found: Parse status code from MockId, return status file content
  - If no status file: Locate regular mock file at `mocks/{ServiceName}/{MockId}.*`
  - Check for optional headers file (`{ServiceName}/{MockId}.headers.json`)
  - Apply status code semantics (204 = no content)
  - Determine Content-Type from mock file extension
  - Add custom headers from headers file (if present)
  - Return response based on file type
- **Errors:**
  - `400 Bad Request`: Missing `X-Mockery-Mock` header, no valid mock IDs, or `X-Mockery-Repo` used in development mode
  - `404 Not Found`: No matching mock file found, or repository name not found (production mode)
  - `500 Internal Server Error`: Unexpected error

**Example Requests:**

*Single Mock ID (default 200 OK):*
```http
GET /api/mock HTTP/1.1
Host: mockery.example.com
X-Mockery-Mock: FooBar/1234
```

*With Repository Selection (Production Mode):*
```http
GET /api/mock HTTP/1.1
Host: mockery.example.com
X-Mockery-Mock: FooBar/1234
X-Mockery-Repo: secondary
```

*Multiple Mock IDs (random selection):*
```http
GET /api/mock HTTP/1.1
Host: mockery.example.com
X-Mockery-Mock: FooBar/1234,FooBar/5678,Products/hydrate
```

*Status File (Gateway Timeout):*
```http
GET /api/mock HTTP/1.1
Host: mockery.example.com
X-Mockery-Mock: FooBar/504
```

**Example Responses:**

*Success (JSON):*
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
    "id": 1234,
    "name": "Sample Item",
    "status": "active"
}
```

*Success with Custom Headers:*
```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Custom-Header: CustomValue
X-Request-ID: abc-123-def-456
Cache-Control: no-cache

{
    "id": 1234,
    "name": "Sample Item",
    "status": "active"
}
```

*Status File Response (HTTP 504):*
```http
HTTP/1.1 504 Gateway Timeout
Content-Type: application/json

{
    "error": "Gateway Timeout",
    "message": "The upstream server did not respond in time"
}
```

*Actual Error - Mock Not Found:*
```http
HTTP/1.1 404 Not Found
Content-Type: application/json

{
    "error": "Mock not found",
    "mockIds": ["FooBar/9999"]
}
```

#### 5.2.2 Mock Management API

The Mock Management API provides CRUD operations for managing mock files. These endpoints allow listing, creating, and deleting mock files programmatically.

**GET /api/mocks**
- **Auth:** None
- **Purpose:** List directories and files at a specified path
- **Headers:**
  - `X-Mockery-Mock: <path>` (optional, default: root "/" )
- **Response:** JSON with folders and files at the path
- **Behavior:**
  - Empty header or "/" returns root directory listing
  - Path can include leading slashes (e.g., `//weather/prod`)
  - Returns folders and files with metadata (name, type, extension, size)
  - **Hidden files/folders excluded:** Items starting with `.` (e.g., `.git`, `.gitignore`) are filtered out

**Example Request:**
```http
GET /api/mocks HTTP/1.1
Host: mockery.example.com
X-Mockery-Mock: weather/prod
```

**Example Response:**
```json
{
  "path": "weather/prod",
  "items": [
    { "name": "success.json", "type": "file", "extension": ".json", "size": 42 },
    { "name": "error.json", "type": "file", "extension": ".json", "size": 128 },
    { "name": "subdir", "type": "folder" }
  ]
}
```

---

**POST /api/mocks**
- **Auth:** None
- **Purpose:** Create a new mock file (**development mode only**)
- **Headers:**
  - `X-Mockery-Mock: <path/filename>` (required, e.g., `weather/prod/success.json`)
- **Body:** File content (raw text/JSON/HTML/etc.)
- **Response:** 
  - `201 Created` with file metadata on success (development mode)
  - `409 Conflict` if file already exists (idempotency check)
  - `501 Not Implemented` in production mode
- **Behavior:**
  - **Production mode:** Returns `501 Not Implemented` immediately
  - **Development mode:**
    - Creates directories if they don't exist
    - **Idempotent:** Returns 409 Conflict if file already exists
- **Errors:**
  - `400 Bad Request`: Missing `X-Mockery-Mock` header or empty body
  - `409 Conflict`: File already exists at specified path
  - `501 Not Implemented`: Production mode (read-only)

**Example Request:**
```http
POST /api/mocks HTTP/1.1
Host: mockery.example.com
X-Mockery-Mock: weather/prod/success.json
Content-Type: application/json

{"temperature": 72, "conditions": "sunny"}
```

**Example Response (Local Mode):**
```json
{
  "path": "weather/prod",
  "fileName": "success.json",
  "size": 42,
  "committedToGit": false
}
```

**Example Error Response (File Exists):**
```http
HTTP/1.1 409 Conflict
Content-Type: application/json

{
  "error": "File already exists: weather/prod/success.json"
}
```

**Example Error Response (Production Mode):**
```http
HTTP/1.1 501 Not Implemented
Content-Type: application/json

{
  "error": "Write operations are not supported in production mode"
}
```

---

**DELETE /api/mocks**
- **Auth:** None
- **Purpose:** Delete a mock file (**development mode only**)
- **Headers:**
  - `X-Mockery-Mock: <path/filename>` (required, e.g., `weather/prod/success.json`)
- **Response:** 
  - `200 OK` with deletion details (development mode)
  - `501 Not Implemented` in production mode
- **Behavior:**
  - **Production mode:** Returns `501 Not Implemented` immediately
  - **Development mode:**
    - Deletes the specified file
    - Deletes empty parent folders up to (but not including) mocks root
- **Errors:**
  - `400 Bad Request`: Missing `X-Mockery-Mock` header
  - `404 Not Found`: File not found
  - `501 Not Implemented`: Production mode (read-only)

**Example Request:**
```http
DELETE /api/mocks HTTP/1.1
Host: mockery.example.com
X-Mockery-Mock: weather/prod/success.json
```

**Example Response (Development Mode):**
```json
{
  "deletedFile": "weather/prod/success.json",
  "deletedFolders": ["weather/prod", "weather"],
  "committedToGit": false
}
```

**Example Error Response (Production Mode):**
```http
HTTP/1.1 501 Not Implemented
Content-Type: application/json

{
  "error": "Write operations are not supported in production mode"
}
```

---

#### 5.2.3 Health Check Endpoints

Mockery uses ASP.NET Core HealthChecks middleware for container orchestration.

**GET /health/live**
- **Purpose:** Liveness probe - always returns healthy if application is running
- **Response:** `200 OK` with `{"status":"Healthy"}`

**GET /health/ready**
- **Purpose:** Readiness probe - checks mock repository accessibility
- **Checks:**
  - Local mode: Verifies mocks directory exists
  - Git mode: Verifies `.git` directory exists (repository cloned)
- **Response:** `200 OK` if ready, `503 Service Unavailable` if not

**GET /health/startup**
- **Purpose:** Startup probe - indicates application startup complete
- **Response:** `200 OK` with `{"status":"Healthy"}`

#### 5.2.3 Metrics Endpoint

**GET /metrics**
- **Purpose:** Prometheus metrics scraping endpoint
- **Format:** Prometheus text format
- **Metrics Include:**
  - ASP.NET Core hosting metrics
  - Kestrel server metrics
  - HTTP request duration and counts
  - Application-specific metrics

#### 5.2.4 Swagger/OpenAPI (Development Only)

**GET /swagger**
- **Purpose:** Swagger UI for API exploration
- **Availability:** Development environment only (`ASPNETCORE_ENVIRONMENT=Development`)

---

## 6. Key Technical Decisions

### 6.1 Kubernetes-Native Git Operations

**Decision:** Use Kubernetes init containers and git-sync sidecars instead of in-process LibGit2Sharp for Git operations in production.

**Rationale:**
- **No Native Dependencies:** LibGit2Sharp requires native libraries that can cause compatibility issues across platforms
- **Separation of Concerns:** Git operations handled by dedicated containers, application focuses on serving mocks
- **Battle-Tested Tooling:** `git-sync` is a mature, well-maintained Kubernetes project from the sig-apps community
- **Simplified Application:** Application becomes stateless file reader, easier to test and maintain
- **Multi-Repository Support:** Easier to configure multiple repositories with separate sidecars
- **Reduced Attack Surface:** Git credentials not handled by application code

**Trade-offs:**
- **Kubernetes Dependency:** Production mode requires Kubernetes; cannot run Git-synced mocks in standalone Docker
- **Read-Only Production:** No real-time write operations; mocks must be updated via Git commits
- **Sync Latency:** Changes take up to sync interval (default 60s) to appear
- **Resource Overhead:** Additional sidecar containers consume memory/CPU

**Implementation:**
- **Init Container:** Alpine + git image clones repos on pod startup
- **Sidecar:** `registry.k8s.io/git-sync/git-sync:v4.4.0` pulls changes periodically
- **ConfigMap:** Repository configuration (URLs, branches, mount paths)
- **Secret:** Personal Access Tokens for Git authentication

### 6.2 Read-Only Production Mode

**Decision:** Production mode is read-only; POST and DELETE endpoints return `501 Not Implemented`.

**Rationale:**
- **Simplicity:** No need for push logic, conflict resolution, or transaction management
- **Git as Source of Truth:** All mock changes go through standard Git workflows (PRs, reviews)
- **Audit Trail:** Every change is a Git commit with author and timestamp
- **Consistency:** All replicas serve identical content from Git
- **Security:** Application cannot modify production data

**Trade-offs:**
- **No Real-Time Updates:** Cannot create/delete mocks via API in production
- **Workflow Change:** Teams must use Git instead of API for mock management in production

### 6.3 Multi-Repository Support

**Decision:** Support multiple Git repositories in production mode via `X-Mockery-Repo` header.

**Rationale:**
- **Team Isolation:** Different teams can manage their own mock repositories
- **Access Control:** Git repository permissions provide natural access boundaries
- **Scalability:** Large organizations can partition mocks across repositories
- **Flexibility:** Different branches/repos for different environments or use cases

**Implementation:**
- ConfigMap defines multiple repositories with unique names
- `X-Mockery-Repo` header selects which repository to query
- `defaultRepository` configuration provides fallback when header not present
- Each repository has its own mount path, git-sync sidecar, and PAT

**Trade-offs:**
- **Complexity:** More configuration than single-repo setup
- **Resource Usage:** Each repository requires its own git-sync sidecar
- **Development Parity:** Development mode only supports single local directory

### 6.4 Git Repository for Mock Storage

### 6.5 Status Files for HTTP Status Codes

**Decision:** Use `.status.json` files with status code in filename instead of request headers.

**Rationale:**
- **File-Based:** Keeps all mock configuration in files (no special headers)
- **Version Controlled:** Status responses versioned alongside regular mocks
- **Reusable:** Same status file can be used across test runs
- **Self-Documenting:** Filename clearly indicates expected status code
- **Simple Client:** Clients just specify mock ID, no extra headers needed

**Implementation:**
```
mocks/FooBar/504.status.json → Returns HTTP 504
mocks/FooBar/200.status.json → Returns HTTP 200
```

**Trade-offs:**
- **Multiple Files:** Need separate file per status code
- **Naming Convention:** Status code must be first part of filename
- **Validation:** Status code extracted from filename must be valid (100-599)

### 6.6 File Extension for Content-Type Detection

**Decision:** Determine HTTP Content-Type header from file extension instead of metadata files or configuration.

**Rationale:**
- **Simplicity:** No separate configuration files or database records
- **Clarity:** Extension immediately indicates response format
- **Standard Practice:** Follows web server conventions (Apache, Nginx)
- **Tooling Support:** Editors provide syntax highlighting based on extension

**Supported Extensions:**
| Extension | Content-Type |
|-----------|-------------|
| `.json` | `application/json` |
| `.html` | `text/html` |
| `.xml` | `application/xml` |
| `.txt` | `text/plain` |
| `.csv` | `text/csv` |
| `.pdf` | `application/pdf` |
| `.js` | `application/javascript` |
| `.css` | `text/css` |
| `.png` | `image/png` |
| `.jpg` / `.jpeg` | `image/jpeg` |
| `.gif` | `image/gif` |
| `.svg` | `image/svg+xml` |
| (default) | `application/octet-stream` |

### 6.7 Optional Headers Files

**Decision:** Support optional `.headers.json` files alongside mock files for custom HTTP response headers.

**Rationale:**
- **Flexibility:** Enable testing of custom headers (authentication, caching, etc.)
- **Simplicity Preserved:** Headers files are optional; simple mocks work without them
- **Separation of Concerns:** Response content in mock file, custom headers in separate file
- **Real-World Testing:** Simulate various HTTP headers for comprehensive testing
- **Backward Compatible:** Existing mocks without headers files work with Content-Type only

**Trade-offs:**
- **Additional Files:** Requires two files for mocks with custom headers
- **Consistency:** Must keep mock and headers files in sync
- **Naming Convention:** Developers must follow `.headers.json` convention

### 6.8 Random Selection from Multiple Mock IDs

**Decision:** Support comma-separated mock IDs in header with random selection.

**Rationale:**
- **Testing Variability:** Simulate different API responses in tests
- **Simplicity:** Single header instead of multiple API calls
- **Chaos Engineering:** Introduce controlled randomness

**Implementation:**
```csharp
var selectedMockId = mockIdList.Count > 1
    ? mockIdList[Random.Shared.Next(mockIdList.Count)]
    : mockIdList[0];
```

**Trade-offs:**
- **Non-Deterministic:** Same request may return different responses
- **No Weighted Selection:** All mock IDs have equal probability

### 6.9 Service Folder Organization

**Decision:** Organize mocks by service folders and require service name prefix in mock IDs.

**Rationale:**
- **Explicit Addressing:** Mock ID includes service name (e.g., `FooBar/1234`)
- **Direct Lookup:** Direct path resolution, no cross-folder searching
- **Performance:** O(1) file lookup per service folder
- **Clarity:** Clear separation of mocks by service
- **Scalability:** Avoid thousands of files in single directory
- **Collision Prevention:** Same file ID can exist in different services

### 6.10 No Authentication

**Decision:** Remove all authentication and authorization mechanisms.

**Rationale:**
- **Simplicity:** No user management, API keys, or JWT validation
- **Testing Focus:** Service intended for development/testing environments
- **Network Security:** Infrastructure-level security sufficient
- **Reduced Dependencies:** No Firebase, no user database

**Security Model:**
- **Network-Level:** Deploy in private networks or behind VPN
- **Infrastructure-Level:** Use Azure Front Door, API Gateway, or firewall rules

### 6.11 OpenTelemetry Integration

**Decision:** Full OpenTelemetry integration for observability.

**Rationale:**
- **Standard Protocol:** OTEL is industry standard for observability
- **Flexible Export:** Support Prometheus, OTLP, Console exporters
- **Comprehensive:** Metrics, traces, and logs in one framework
- **Cloud Native:** Works with Aspire, Jaeger, Zipkin, etc.

**Implementation:**
- Prometheus scraping endpoint at `/metrics`
- OTLP export when `OTEL_EXPORTER_OTLP_ENDPOINT` configured
- Console fallback for local development
- ASP.NET Core and HTTP client instrumentation

---

## 7. Dependencies

### 7.1 External Services

| Service | Version | Purpose | Criticality | Mode |
|---------|---------|---------|-------------|------|
| **Git Repository** | 2.0+ | Mock file storage and version control | Critical | Production Only |
| **Local File System** | N/A | Mock file storage for local development | Critical | Development Only |
| **OTLP Endpoint** | OTEL 1.0+ | Centralized telemetry collection | Optional | Both |
| **Kubernetes** | 1.25+ | Container orchestration with init containers and sidecars | Critical | Production Only |

### 7.2 Internal Services

None (monolithic architecture)

### 7.3 Libraries and Frameworks

#### 7.3.1 NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| **Microsoft.AspNetCore.App** | 9.0.0 | ASP.NET Core framework |
| **Microsoft.AspNetCore.OpenApi** | 9.0.0 | OpenAPI support |
| **Swashbuckle.AspNetCore** | 6.9.0 | Swagger UI |
| **OpenTelemetry.Extensions.Hosting** | 1.10.0 | OTEL hosting integration |
| **OpenTelemetry.Instrumentation.AspNetCore** | 1.10.0 | ASP.NET Core instrumentation |
| **OpenTelemetry.Instrumentation.Http** | 1.10.0 | HTTP client instrumentation |
| **OpenTelemetry.Exporter.Console** | 1.10.0 | Console exporter |
| **OpenTelemetry.Exporter.OpenTelemetryProtocol** | 1.10.0 | OTLP exporter |
| **OpenTelemetry.Exporter.Prometheus.AspNetCore** | 1.10.0-beta.1 | Prometheus exporter |

**Removed in v5.0:**

| Package | Version | Reason |
|---------|---------|--------|
| ~~LibGit2Sharp~~ | ~~0.30.0~~ | Replaced by Kubernetes git-sync sidecar |

#### 7.3.2 Testing Frameworks

| Package | Version | Purpose |
|---------|---------|---------|
| **xUnit** | Latest | Unit testing framework |
| **Moq** | Latest | Mocking framework |
| **FluentAssertions** | Latest | Assertion library |
| **Microsoft.AspNetCore.Mvc.Testing** | Latest | Integration testing |

---

## 8. Architecture Diagrams

### 8.1 System Context Diagram

```mermaid
graph TB
    subgraph "Development"
        DevClient[Client Application]
        DevMockery[Mockery REST API]
        DevFS[Local File System<br/>./mocks/]
    end

    subgraph "Production (Kubernetes)"
        ProdClient[Client Application]
        
        subgraph "Pod"
            InitContainer[Init Container<br/>git clone]
            MockeryAPI[Mockery REST API]
            GitSync1[git-sync Sidecar<br/>Repository 1]
            GitSync2[git-sync Sidecar<br/>Repository 2]
        end
        
        ProdFS[Shared Volumes<br/>/app/mocks/*]
        GitRepo1[Git Repository 1]
        GitRepo2[Git Repository 2]
        ConfigMap[ConfigMap<br/>Repo Config]
        Secret[Secret<br/>PATs]
    end
    
    OTEL[OpenTelemetry Collector]

    DevClient -->|GET /api/mock<br/>X-Mockery-Mock| DevMockery
    DevMockery -->|Read/Write| DevFS
    
    ProdClient -->|GET /api/mock<br/>X-Mockery-Mock<br/>X-Mockery-Repo| MockeryAPI
    InitContainer -->|Clone on startup| ProdFS
    MockeryAPI -->|Read only| ProdFS
    GitSync1 -->|Periodic pull| ProdFS
    GitSync2 -->|Periodic pull| ProdFS
    GitSync1 -.->|Fetch| GitRepo1
    GitSync2 -.->|Fetch| GitRepo2
    InitContainer -.->|Read config| ConfigMap
    InitContainer -.->|Read PATs| Secret
    GitSync1 -.->|Read PAT| Secret
    GitSync2 -.->|Read PAT| Secret
    
    DevMockery -.->|Metrics/Traces/Logs| OTEL
    MockeryAPI -.->|Metrics/Traces/Logs| OTEL

    style MockeryAPI fill:#326CE5,stroke:#fff,stroke-width:2px,color:#fff
    style DevMockery fill:#326CE5,stroke:#fff,stroke-width:2px,color:#fff
    style GitRepo1 fill:#F05032,stroke:#fff,stroke-width:2px,color:#fff
    style GitRepo2 fill:#F05032,stroke:#fff,stroke-width:2px,color:#fff
    style GitSync1 fill:#00A6ED,stroke:#fff,stroke-width:2px,color:#fff
    style GitSync2 fill:#00A6ED,stroke:#fff,stroke-width:2px,color:#fff
    style InitContainer fill:#FFA000,stroke:#fff,stroke-width:2px,color:#000
```

### 8.2 Component Architecture Diagram

```mermaid
graph TB
    subgraph "Presentation Layer"
        MockCtrl[MockController]
        MocksCtrl[MocksController]
        HealthChecks[Health Check Endpoints]
        Swagger[Swagger UI]
        Prometheus[Prometheus Endpoint]
    end

    subgraph "Business Logic Layer"
        MockService[MockService]
        MocksMgmtService[MocksManagementService]
        ContentTypeResolver[ContentTypeResolver]
    end

    subgraph "Repository Layer"
        RepoInterface[IMockRepository]
        BaseRepo[FileSystemMockRepositoryBase]
        LocalRepo[LocalFileMockRepository<br/>Development - R/W]
        MultiRepo[MultiRepoFileMockRepository<br/>Production - Read Only]
    end

    subgraph "Infrastructure (Development)"
        DevFS[Local File System<br/>./mocks/]
    end

    subgraph "Infrastructure (Production - Kubernetes)"
        InitContainer[Init Container]
        GitSync[git-sync Sidecars]
        ProdFS[Shared Volumes]
        GitRemote[Git Repositories]
    end

    MockCtrl --> MockService
    MocksCtrl --> MocksMgmtService
    MockService --> RepoInterface
    MocksMgmtService --> RepoInterface
    MockService --> ContentTypeResolver

    RepoInterface --> BaseRepo
    BaseRepo --> LocalRepo
    BaseRepo --> MultiRepo

    LocalRepo --> DevFS
    MultiRepo --> ProdFS
    
    InitContainer -->|Clone| ProdFS
    GitSync -->|Pull| ProdFS
    InitContainer -.->|Fetch| GitRemote
    GitSync -.->|Fetch| GitRemote

    style MockService fill:#326CE5,stroke:#fff,stroke-width:2px,color:#fff
    style BaseRepo fill:#00A6ED,stroke:#fff,stroke-width:2px,color:#fff
    style LocalRepo fill:#4CAF50,stroke:#fff,stroke-width:2px,color:#fff
    style MultiRepo fill:#FF9800,stroke:#fff,stroke-width:2px,color:#000
    style InitContainer fill:#FFA000,stroke:#fff,stroke-width:2px,color:#000
    style GitSync fill:#00A6ED,stroke:#fff,stroke-width:2px,color:#fff
```

### 8.3 Mock Retrieval Sequence Diagram (Production with Multi-Repo)

```mermaid
sequenceDiagram
    participant Client
    participant Controller as MockController
    participant Service as MockService
    participant Repo as MultiRepoFileMockRepository
    participant FS as FileSystem (/app/mocks/secondary)

    Client->>Controller: GET /api/mock<br/>X-Mockery-Mock: FooBar/504<br/>X-Mockery-Repo: secondary
    Controller->>Controller: Parse headers
    Controller->>Service: GetMockAsync(["FooBar/504"], "secondary")

    Service->>Repo: ResolveRepository("secondary")
    Repo->>Repo: Find repo config by name
    Repo-->>Service: mountPath=/app/mocks/secondary

    Service->>Service: Parse: service=FooBar, fileId=504
    Service->>Repo: FindHeadersFileAsync("FooBar", "504")
    Repo->>FS: Check /app/mocks/secondary/FooBar/504.headers.json
    FS-->>Repo: null (not found)
    Repo-->>Service: null

    Service->>Repo: FindStatusFileAsync("FooBar", "504")
    Repo->>FS: Check /app/mocks/secondary/FooBar/504.status.json
    FS-->>Repo: File exists
    Repo->>Repo: Parse status code from "504"
    Repo->>FS: Read file content
    FS-->>Repo: JSON content
    Repo-->>Service: (504, content)

    Service->>Service: StatusCode=504, ShouldReturnContent=true
    Service-->>Controller: MockFileResult

    Controller->>Controller: Set status code 504
    Controller-->>Client: HTTP 504<br/>Content-Type: application/json<br/>Body: error JSON
```

### 8.4 Kubernetes Pod Structure Diagram

```mermaid
graph TB
    subgraph "Kubernetes Pod"
        subgraph "Init Containers (sequential)"
            Init[git-clone-init<br/>alpine/git:latest]
        end
        
        subgraph "Containers (parallel)"
            Main[mockery<br/>Main Application]
            Sync1[git-sync-primary<br/>git-sync:v4.4.0]
            Sync2[git-sync-secondary<br/>git-sync:v4.4.0]
        end
    end
    
    subgraph "Volumes"
        Vol1[mocks-primary<br/>emptyDir]
        Vol2[mocks-secondary<br/>emptyDir]
        ConfigVol[config-volume<br/>ConfigMap]
        SecretVol[secrets-volume<br/>Secret]
    end
    
    subgraph "External"
        GitRepo1[GitHub Repo 1]
        GitRepo2[GitHub Repo 2]
    end

    Init -->|mount| Vol1
    Init -->|mount| Vol2
    Init -->|read| ConfigVol
    Init -->|read| SecretVol
    Init -.->|clone| GitRepo1
    Init -.->|clone| GitRepo2
    
    Main -->|read /app/mocks/primary| Vol1
    Main -->|read /app/mocks/secondary| Vol2
    Main -->|read| ConfigVol
    
    Sync1 -->|write| Vol1
    Sync1 -->|read| SecretVol
    Sync1 -.->|pull| GitRepo1
    
    Sync2 -->|write| Vol2
    Sync2 -->|read| SecretVol
    Sync2 -.->|pull| GitRepo2

    style Main fill:#326CE5,stroke:#fff,stroke-width:2px,color:#fff
    style Init fill:#FFA000,stroke:#fff,stroke-width:2px,color:#000
    style Sync1 fill:#00A6ED,stroke:#fff,stroke-width:2px,color:#fff
    style Sync2 fill:#00A6ED,stroke:#fff,stroke-width:2px,color:#fff
```

### 8.5 File Organization Diagram

```mermaid
graph TB
    subgraph "Repository Structure"
        Root[mocks/]

        FooBar[FooBar/]
        Mock1234[1234.json]
        Headers1234[1234.headers.json]
        Status200[200.status.json]
        Status504[504.status.json]
        Mock5678[5678.html]

        Products[Products/]
        MockHydrate[hydrate.json]
        MockError[error.json]

        Root --> FooBar
        Root --> Products

        FooBar --> Mock1234
        FooBar --> Headers1234
        FooBar --> Status200
        FooBar --> Status504
        FooBar --> Mock5678

        Products --> MockHydrate
        Products --> MockError
    end

    style Root fill:#F05032,stroke:#fff,stroke-width:2px,color:#fff
    style FooBar fill:#FFA000,stroke:#fff,stroke-width:2px,color:#000
    style Products fill:#FFA000,stroke:#fff,stroke-width:2px,color:#000
```

---

## 9. Cross-Cutting Concerns

### 9.1 Security

#### 9.1.1 Authentication & Authorization

**No Authentication:** Service has no built-in authentication.

**Security Model:**
- **Network-Level Security:** Deploy in private networks, behind VPN, or with IP whitelisting
- **Environment Separation:** Intended for development/testing environments

#### 9.1.2 Input Validation

- **Mock ID Format:** Validate contains `/` separator for service/fileId parsing
- **Path Traversal Prevention:** File operations restricted to mocks directory
- **Status Code Validation:** Must be valid HTTP status code (100-599)

#### 9.1.3 Known Security Considerations

1. **Public Access:** Service accessible if exposed to internet
   - Mitigation: Deploy in private network

2. **Git Credentials:** Access token stored in configuration
   - Mitigation: Use Kubernetes secrets, environment variables

3. **Path Traversal:** Mock IDs could attempt directory traversal
   - Mitigation: Parse service/fileId, restrict to mocks directory

### 9.2 Performance

#### 9.2.1 Expected Load

- **Concurrent Users:** 100 concurrent testing environments
- **Requests per Second:** 1000 requests/second (peak)
- **Mock Files:** 1000 mock files across 50 service folders
- **Average Mock Size:** 10 KB

#### 9.2.2 Latency Requirements

| Operation | Target (p95) | Notes |
|-----------|--------------|-------|
| Mock Retrieval | < 50ms | File system read |
| Random Selection | < 100ms | Selection + file read |
| Git Refresh | < 5 seconds | Async, non-blocking |
| Startup (Git Clone) | < 30 seconds | One-time |

#### 9.2.3 Optimization Strategies

- **OS File Caching:** Operating system caches frequently accessed files
- **Random.Shared:** Thread-safe random selection
- **Async Operations:** Non-blocking file I/O
- **Background Refresh:** Git pull doesn't block requests

### 9.3 Scalability

#### 9.3.1 Horizontal Scaling

- **Stateless API:** No in-memory session state
- **Independent Clones:** Each instance maintains own Git clone

#### 9.3.2 Kubernetes Scaling

```yaml
spec:
  replicas: 2
  resources:
    requests:
      memory: "256Mi"
      cpu: "100m"
    limits:
      memory: "512Mi"
      cpu: "500m"
```

### 9.4 Monitoring & Observability

#### 9.4.1 Health Checks

- `/health/live` - Liveness probe
- `/health/ready` - Readiness probe (checks repository)
- `/health/startup` - Startup probe

#### 9.4.2 Metrics (Prometheus)

- `/metrics` - Prometheus scraping endpoint
- ASP.NET Core metrics
- Kestrel server metrics
- HTTP client metrics

#### 9.4.3 Logging

- Console logging with structured format
- OpenTelemetry log export (when configured)
- Log levels configurable per environment

#### 9.4.4 Tracing

- OpenTelemetry distributed tracing
- ASP.NET Core instrumentation
- HTTP client instrumentation
- OTLP export to Aspire Dashboard or other collectors

### 9.5 Error Handling

**Standard HTTP Status Codes:**
- `200 OK`: Mock file found and returned
- `400 Bad Request`: Missing or invalid `X-Mockery-Mock` header
- `404 Not Found`: Mock file not found
- `500 Internal Server Error`: Unhandled exception

**Error Response Format:**
```json
{
    "error": "Error message",
    "mockIds": ["FooBar/9999"]
}
```

### 9.6 CORS Configuration

**Default Configuration:** Permissive (allows all origins)

```csharp
policy.AllowAnyOrigin()
      .AllowAnyMethod()
      .AllowAnyHeader();
```

**Note:** Acceptable for internal testing service; restrict for production if needed.

---

## 10. Testing Strategy

### 10.1 Unit Testing

**Framework:** xUnit with Moq and FluentAssertions

**Test Structure:**
```
src/Mockery.Test/
├── Controllers/
│   ├── MockControllerTests.cs
│   └── MocksControllerTests.cs
├── Services/
│   ├── MockServiceTests.cs
│   └── ContentTypeResolverTests.cs
└── Repository/
    ├── LocalFileMockRepositoryTests.cs
    └── MultiRepoFileMockRepositoryTests.cs
```

**Removed in v5.0:**
- ~~`GitMockRepositoryTests.cs`~~ - GitMockRepository removed
- ~~`GitRepositoryRefreshServiceTests.cs`~~ - GitRepositoryRefreshService removed

**Total Tests:** 95

**Coverage Areas:**
- Mock service business logic
- Mock management service (list, create, delete)
- Content-type resolution
- Random selection
- Repository file operations
- Controller HTTP handling

### 10.2 Integration Testing

**WebApplicationFactory:** In-process API testing

```csharp
public class MockControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetMock_WithValidMockId_Returns200()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Mockery-Mock", "FooBar/1234");
        
        var response = await client.GetAsync("/api/mock");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

### 10.3 Test Environments

| Environment | Purpose | Repository Mode |
|-------------|---------|-----------------|
| **Local** | Developer testing | Local (read/write) |
| **CI/CD** | Automated tests | Local (test fixtures) |
| **Staging** | Pre-production | MultiRepo (read-only) |
| **Production** | Live | MultiRepo (read-only) |

---

## 11. CI/CD & Deployment

### 11.1 Container Build

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# ... build steps ...

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/mocks
ENTRYPOINT ["dotnet", "Mockery.dll"]
```

**Port:** Application runs on port 8080

**Multi-Architecture Support:**
The CI/CD pipeline builds Docker images for both `linux/amd64` (Windows/Intel) and `linux/arm64` (Apple Silicon Mac). This enables running Mockery on:
- Windows development machines (amd64)
- Apple Silicon Macs (M1/M2/M3/M4) (arm64)
- Cloud Kubernetes clusters (typically amd64)

The multi-arch manifest allows Docker to automatically pull the correct architecture variant.

### 11.2 Helm Chart Deployment

**Chart Location:** `charts/mockery/`

**Values Configuration:**
```yaml
name: mockery
namespace: dev
replicaCount: 2

image:
  name: mockery-mockery:latest
  pullPolicy: Always

service:
  type: ClusterIP
  port: 80
  targetPort: 8080

config:
  aspnetcoreEnvironment: "Production"
  openTelemetry:
    serviceName: "Mockery"
    otlpEndpoint: "http://aspire.monitor.svc.cluster.local:18889"
    otlpProtocol: "grpc"
    otlpApiKey: "aspire"

# Multi-repository configuration (v5.0+)
gitRepositories:
  defaultRepository: "primary"
  repositories:
    - name: "primary"
      url: "https://github.com/org/mocks-primary.git"
      branch: "main"
      mountPath: "/app/mocks/primary"
      secretKeyRef: "primary-pat"
    - name: "secondary"
      url: "https://github.com/org/mocks-secondary.git"
      branch: "main"
      mountPath: "/app/mocks/secondary"
      secretKeyRef: "secondary-pat"

# git-sync sidecar configuration
gitSync:
  image: registry.k8s.io/git-sync/git-sync:v4.4.0
  period: "60s"
  depth: 1

# Init container configuration
initContainer:
  image: alpine/git:latest
```

**Helm Templates (v5.0 Changes):**

| Template | Changes |
|----------|--------|
| `deployment.yaml` | Added init container, git-sync sidecars, multiple volume mounts |
| `configmap.yaml` | Added `repositories.json` with multi-repo configuration |
| `secret.yaml` | PATs keyed by `secretKeyRef` names |
| `pvc.yaml` | Removed (using emptyDir volumes per repo) |

### 11.3 Environment Configuration

| Environment | Mode | Config File |
|-------------|------|-------------|
| Development | Local | `appsettings.Development.json` |
| Production | MultiRepo | `appsettings.Production.json` |

**Development Settings:**
```json
{
  "MockRepository": {
    "Type": "Local",
    "LocalPath": "../.."  // Relative to src/Mockery/
  }
}
```

**Production Settings:**
```json
{
  "MockRepository": {
    "Type": "MultiRepo"
    // Repository configuration loaded from ConfigMap
    // See Section 3.2.6.3 for ConfigMap schema
  }
}
```

**Kubernetes ConfigMap for Production:**

The multi-repository configuration is provided via Kubernetes ConfigMap (see Section 3.2.6.3). The application reads this configuration on startup to determine available repositories and the default repository.

**Kubernetes Secret for PATs:**

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: mockery-git-secrets
type: Opaque
stringData:
  primary-pat: ghp_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
  secondary-pat: ghp_yyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyyy
```

**Note:** PATs require `repo` scope for private repositories or `public_repo` for public repositories.

---

## 12. Development Workflow

### 12.1 Local Development Setup

**Prerequisites:**
- .NET 9.0 SDK
- Git
- Visual Studio 2022 or VS Code

**Quick Start (Local Mode):**
```bash
git clone https://github.com/your-org/mockery.git
cd mockery/src/Mockery
dotnet restore
dotnet run
```

**Test Endpoint:**
```bash
curl -i -H "X-Mockery-Mock: FooBar/1234" http://localhost:8080/api/mock
```

### 12.2 Adding Mocks

**Local Development:**
```bash
mkdir -p mocks/MyService
echo '{"status":"success"}' > mocks/MyService/test.json
curl -i -H "X-Mockery-Mock: MyService/test" http://localhost:8080/api/mock
```

**With Status Code:**
```bash
echo '{"error":"Not Found"}' > mocks/MyService/404.status.json
curl -i -H "X-Mockery-Mock: MyService/404" http://localhost:8080/api/mock
# Returns HTTP 404 with error JSON
```

**With Custom Headers:**
```bash
echo '{"X-Custom":"Value"}' > mocks/MyService/test.headers.json
```

### 12.3 Project Structure

```
src/
├── Mockery/
│   ├── Controllers/
│   │   ├── MockController.cs
│   │   └── MocksController.cs
│   ├── BusinessLogic/
│   │   ├── IMockService.cs
│   │   ├── MockService.cs
│   │   ├── IMocksManagementService.cs
│   │   └── MocksManagementService.cs
│   ├── Repository/
│   │   ├── IMockRepository.cs
│   │   ├── FileSystemMockRepositoryBase.cs
│   │   ├── LocalFileMockRepository.cs
│   │   └── MultiRepoFileMockRepository.cs
│   ├── Models/
│   │   ├── MockFileResult.cs
│   │   ├── DirectoryListingResponse.cs
│   │   ├── CreateMockResponse.cs
│   │   └── DeleteMockResponse.cs
│   ├── Services/
│   │   ├── ContentTypeResolver.cs
│   │   └── MockeryMetrics.cs
│   ├── Extensions/OpenTelemetryExtensions.cs
│   ├── Configuration/
│   │   ├── MockRepositorySettings.cs
│   │   └── MultiRepoSettings.cs
│   └── Program.cs
├── Mockery.Test/
│   ├── Controllers/
│   │   ├── MockControllerTests.cs
│   │   └── MocksControllerTests.cs
│   ├── Services/
│   └── Repository/
├── .http/                          # HTTP request files for API testing
│   ├── requests-local.http
│   ├── requests-docker.http
│   └── requests-production.http
└── mocks/
    ├── FooBar/
    ├── Products/
    ├── mockery/
    └── test/
```

**Removed in v5.0:**
- ~~`Repository/IGitMockRepository.cs`~~ → Replaced by `IMockRepository.cs`
- ~~`Repository/GitMockRepository.cs`~~ → Replaced by `MultiRepoFileMockRepository.cs`
- ~~`Services/GitRepositoryRefreshService.cs`~~ → Replaced by Kubernetes git-sync sidecar
- ~~`Configuration/GitRepositoryOptions.cs`~~ → Replaced by `MultiRepoSettings.cs`

---

## 13. Future Considerations

### 13.1 Potential Improvements

**Short-Term:**
- In-memory cache for frequently accessed mocks
- Mock file index for faster lookups
- Webhook-triggered git-sync refresh

**Medium-Term:**
- Enhanced headers support
- Mock validation on sync
- Rate limiting middleware (if needed)
- Health check for git-sync sidecar status

**Long-Term:**
- Mock versioning via Git tags
- Multi-branch support per repository
- Write API in production (via separate commit service)

### 13.2 Technical Debt

- No in-memory caching layer
- Permissive CORS configuration
- ~~LibGit2Sharp native dependencies~~ (resolved in v5.0)

---

## 14. References

### 14.1 External Documentation

- [ASP.NET Core 9.0](https://docs.microsoft.com/aspnet/core/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Prometheus](https://prometheus.io/docs/)
- [git-sync](https://github.com/kubernetes/git-sync) - Kubernetes sidecar for Git sync
- [Kubernetes Init Containers](https://kubernetes.io/docs/concepts/workloads/pods/init-containers/)
- [Kubernetes Sidecar Containers](https://kubernetes.io/docs/concepts/workloads/pods/sidecar-containers/)

### 14.2 Project Files

- `README.md` - Project overview
- `mocks/README.md` - Mock file documentation
- `charts/mockery/` - Helm chart for Kubernetes deployment
- `.http/` - HTTP request files for API testing
- `.github/instructions/` - GitHub Copilot workspace instructions

---

## Appendix A: Glossary

| Term | Definition |
|------|------------|
| **Mock** | HTTP response content stored as file |
| **Mock ID** | Service-prefixed identifier: `{ServiceName}/{FileId}` |
| **Service Name** | Name of service folder containing the mock |
| **File ID** | Unique identifier for mock within a service |
| **Headers File** | Optional `.headers.json` file with custom HTTP headers |
| **Status File** | `.status.json` file with status code in filename |
| **X-Mockery-Mock** | Required request header containing mock ID(s) |
| **X-Mockery-Repo** | Optional request header to select repository (production mode only) |
| **Local Mode** | Development mode using local file system (read/write) |
| **MultiRepo Mode** | Production mode using multiple git-synced repositories (read-only) |
| **Init Container** | Kubernetes container that runs before main container to clone Git repos |
| **git-sync Sidecar** | Kubernetes sidecar container that periodically pulls Git changes |

---

**Document History:**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 3.0 | 2025-11-16 | System Architecture Team | Dual-mode repository support, Strategy pattern |
| 3.1 | 2025-11-25 | System Architecture Team | Documentation updates |
| 3.2 | 2025-11-25 | System Architecture Team | Updated to reflect actual implementation: removed X-Mock-StatusCode header (replaced by .status.json files), added OpenTelemetry observability, updated content types, corrected port to 8080, updated LibGit2Sharp to 0.30.0, added Helm chart documentation, updated configuration structure |
| 3.3 | 2025-12-13 | System Architecture Team | Removed rate limiting references (not implemented in codebase), updated project structure to match actual implementation |
| 3.4 | 2025-12-14 | System Architecture Team | Updated Docker Compose port mapping from 3000 to 5500, moved memory bank to .clinerules/memory-bank |
| 3.5 | 2025-12-17 | System Architecture Team | Added Mock Management API (GET/POST/DELETE /api/mocks) for listing, creating, and deleting mock files. Git mode auto-commits and pushes changes. Total tests: 89. |
| 3.6 | 2025-12-18 | System Architecture Team | Added idempotency check for POST /api/mocks (returns 409 Conflict if file exists). Added Git access token configuration documentation. Fixed Git staging path for commit/push. Total tests: 91. |
| 3.7 | 2025-12-18 | System Architecture Team | Fixed DELETE /api/mocks Git staging - delete operations now use `Commands.Remove()` instead of `Commands.Stage()` since the file no longer exists after deletion. Total tests: 91. |
| 3.8 | 2025-12-18 | System Architecture Team | GET /api/mocks now filters out hidden files and folders (items starting with `.` like `.git`, `.gitignore`). Total tests: 95. |
| 3.9 | 2025-12-20 | System Architecture Team | Added multi-architecture Docker builds (linux/amd64 + linux/arm64) for cross-platform support on Windows and Apple Silicon Macs. |
| 4.0 | 2026-01-24 | System Architecture Team | Documentation refresh: verified all components match implementation, Helm chart at v0.1.51, added .http request files for API testing, updated GitHub Copilot workspace configuration. Total tests: 95. |
| 5.0 | 2026-01-24 | System Architecture Team | **MAJOR REFACTOR:** Removed LibGit2Sharp dependency. Production mode now uses Kubernetes init container (git clone) + git-sync sidecar (periodic pull). Added multi-repository support with `X-Mockery-Repo` header. Production mode is read-only (POST/DELETE return 501). Removed `GitMockRepository`, `GitRepositoryRefreshService`, `IGitMockRepository`, `GitRepositoryOptions`. Added `MultiRepoFileMockRepository`, `IMockRepository`, `MultiRepoSettings`. Updated Helm chart with init container and sidecar configurations. |
