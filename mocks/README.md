# Mock Files Directory

This directory contains sample mock files for local development and testing.

## Quick Start

**For Local Development (Recommended):**
- Mocks are stored in this `mocks/` directory
- Changes are picked up immediately (no restart needed)
- No Git setup required

**For Production/Git Mode:**
- Create a separate Git repository for your mocks
- Configure Mockery to clone/pull from that repository
- See [Production Setup](#production-setup) below

## Directory Structure

```
mocks/
├── {ServiceName}/
│   ├── {FileId}.{extension}         # Mock content file (e.g., .json, .html, .xml)
│   ├── {FileId}.headers.json        # Optional custom headers file
│   └── {StatusCode}.status.json     # Optional status code file (e.g., 404.status.json)
```

## Mock ID Format

Mock IDs follow the pattern: `{Path}/{FileId}`

- **Path**: Directory path to the mock file (supports subfolders)
- **FileId**: File name without extension (always the last segment after the final `/`)
- **Extension**: Determines the Content-Type header

### Examples

| Mock ID | Path | FileId |
|---------|------|--------|
| `FooBar/1234` | `FooBar` | `1234` |
| `FooBar/Staging/1234` | `FooBar/Staging` | `1234` |
| `FooBar/Staging/Private/test` | `FooBar/Staging/Private` | `test` |

This allows you to organize mocks by environment, feature, or any other hierarchy:

```
mocks/
├── FooBar/
│   ├── 1234.json                    # X-Mock-ID: FooBar/1234
│   ├── Staging/
│   │   ├── 1234.json                # X-Mock-ID: FooBar/Staging/1234
│   │   └── Private/
│   │       └── test.json            # X-Mock-ID: FooBar/Staging/Private/test
│   └── Production/
│       └── 1234.json                # X-Mock-ID: FooBar/Production/1234
```

## Mock File Types

Mockery supports several file types that work together to provide flexible mock responses:

### Overview

| File Pattern | Purpose | Required | Example |
|--------------|---------|----------|---------|
| `{id}.{ext}` | Response body content | Yes | `1234.json`, `user.html` |
| `{id}.headers.json` | Custom HTTP headers | No | `1234.headers.json` |
| `{id}.status.json` | HTTP status code + optional body | No | `404.status.json`, `500.status.json` |

### Content Files (`{id}.{extension}`)

The primary mock file containing the response body. The file extension determines the `Content-Type` header.

**Example:** `mocks/FooBar/1234.json`
```json
{
  "id": 1234,
  "name": "Sample Item",
  "status": "active"
}
```

### Headers Files (`{id}.headers.json`)

Optional companion file that adds custom HTTP response headers.

**Example:** `mocks/FooBar/1234.headers.json`
```json
{
  "X-Custom-Header": "CustomValue",
  "X-Request-ID": "abc-123-def-456",
  "Cache-Control": "no-cache"
}
```

### Status Files (`{statusCode}.status.json`)

Special file type that returns a specific HTTP status code based on the filename. The status code is extracted from the first part of the filename.

**Example:** `mocks/FooBar/504.status.json`
```json
{"error": "Gateway Timeout", "message": "The upstream server did not respond in time"}
```

**Usage:**
```bash
# Returns HTTP 504 with the JSON error body
curl -i -H "X-Mock-ID: FooBar/504" http://localhost:8080/api/mock
```

**Common Status Files:**
| Status Code | File Name | Use Case |
|-------------|-----------|----------|
| `400` | `400.status.json` | Bad Request errors |
| `401` | `401.status.json` | Unauthorized errors |
| `403` | `403.status.json` | Forbidden errors |
| `404` | `404.status.json` | Not Found errors |
| `500` | `500.status.json` | Internal Server Error |
| `503` | `503.status.json` | Service Unavailable |
| `504` | `504.status.json` | Gateway Timeout |

### Status Code Priority

1. **`.status.json` file** - Status code from the filename
2. **Default 200 OK** - When no status is specified

### File Resolution Order

When you request `X-Mock-ID: FooBar/504`:

1. **Status file first:** `mocks/FooBar/504.status.json` → Returns with HTTP 504
2. **Content file second:** `mocks/FooBar/504.json` → Returns with HTTP 200
3. **Not found:** Returns HTTP 404

## Included Sample Mocks

### FooBar Service Examples

- **FooBar/1234** - JSON response with custom headers
  - `FooBar/1234.json` - Simple success response
  - `FooBar/1234.headers.json` - Custom headers (X-Custom-Header, X-Request-ID, etc.)

- **FooBar/5678** - HTML response
  - `FooBar/5678.html` - Sample HTML page

- **FooBar/200** - Status file example (HTTP 200)
  - `FooBar/200.status.json` - Returns HTTP 200 with custom JSON body

- **FooBar/504** - Status file example (Gateway Timeout)
  - `FooBar/504.status.json` - Returns HTTP 504 with error JSON body

### Products Service Examples

- **Products/hydrate** - Product catalog response
  - `Products/hydrate.json` - List of products with pagination

- **Products/error** - Error response example
  - `Products/error.json` - Error message with details

## Testing Locally

To test these mocks locally:

```bash
# Single mock ID (default 200 OK)
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:8080/api/mock

# Status file for error response (HTTP 504)
curl -i -H "X-Mock-ID: FooBar/504" http://localhost:8080/api/mock

# Random selection from multiple IDs
curl -i -H "X-Mock-ID: FooBar/1234,FooBar/5678,Products/hydrate" http://localhost:8080/api/mock
```

## Adding New Mocks (Local Development)

1. Create a service folder if it doesn't exist: `mkdir -p mocks/MyService`
2. Create your mock file: `echo '{"data":"value"}' > mocks/MyService/mockid.json`
3. (Optional) Add custom headers: `echo '{"X-Custom":"Value"}' > mocks/MyService/mockid.headers.json`
4. Test immediately - changes are picked up automatically!

```bash
curl -i -H "X-Mock-ID: MyService/mockid" http://localhost:8080/api/mock
```

## Supported File Extensions

- `.json` → `application/json`
- `.html` → `text/html`
- `.xml` → `application/xml`
- `.txt` → `text/plain`
- And more (see `ContentTypeResolver` for full list)

---

## Production Setup

This section is for teams deploying Mockery in production/staging with Git-based mock management.

### Why Use a Separate Git Repository?

For production deployments, you should:
- Create a separate Git repository for mock files
- Version control your mock responses
- Share mocks across team members via Git workflows
- Deploy Mockery configured to pull from that repository

### Example Production Mock Repository Structure

Create a new Git repository with this structure:

```
mockery-mocks/
├── README.md
└── mocks/
    ├── UserService/
    │   ├── get-user.json
    │   ├── get-user.headers.json
    │   ├── create-user.json
    │   ├── user-not-found.json
    │   └── user-not-found.headers.json
    ├── ProductService/
    │   ├── list-products.json
    │   ├── product-details.json
    │   ├── product-details.headers.json
    │   └── error-response.json
    └── PaymentService/
        ├── success.json
        ├── failed.json
        └── pending.json
```

### Example Mock Files for Production

**UserService/get-user.json:**
```json
{
  "id": 12345,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "role": "admin",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**UserService/get-user.headers.json:**
```json
{
  "X-User-ID": "12345",
  "X-Request-ID": "abc-123-def",
  "Cache-Control": "max-age=3600"
}
```

**ProductService/list-products.json:**
```json
{
  "products": [
    {
      "id": 1,
      "name": "Widget Pro",
      "price": 29.99,
      "inStock": true
    },
    {
      "id": 2,
      "name": "Gadget Plus",
      "price": 49.99,
      "inStock": false
    },
    {
      "id": 3,
      "name": "Tool Master",
      "price": 19.99,
      "inStock": true
    }
  ],
  "total": 3,
  "page": 1
}
```

**PaymentService/success.json:**
```json
{
  "transactionId": "txn_123456789",
  "status": "completed",
  "amount": 99.99,
  "currency": "USD",
  "timestamp": "2024-11-15T14:35:00Z"
}
```

### Production Usage Examples

**Get User (200 OK):**
```bash
curl -i -H "X-Mock-ID: UserService/get-user" http://localhost:8080/api/mock
```

Response includes custom headers:
- `Content-Type: application/json`
- `X-User-ID: 12345`
- `X-Request-ID: abc-123-def`
- `Cache-Control: max-age=3600`

**User Not Found (404) - using status file:**
```bash
# Create 404.status.json in UserService folder
curl -i -H "X-Mock-ID: UserService/404" http://localhost:8080/api/mock
```

**Server Error (500) - using status file:**
```bash
# Create 500.status.json in ProductService folder
curl -i -H "X-Mock-ID: ProductService/500" http://localhost:8080/api/mock
```

**Random Selection:**
```bash
curl -i -H "X-Mock-ID: PaymentService/success,PaymentService/failed,PaymentService/pending" \
     http://localhost:8080/api/mock
```

### Creating Your Production Mock Repository

1. Create a new Git repository:
```bash
mkdir mockery-mocks
cd mockery-mocks
git init
```

2. Create the directory structure:
```bash
mkdir -p mocks/UserService
mkdir -p mocks/ProductService
mkdir -p mocks/PaymentService
```

3. Add your mock files (examples above)

4. Commit and push:
```bash
git add .
git commit -m "Initial mock files"
git remote add origin https://github.com/your-org/mockery-mocks.git
git push -u origin main
```

5. Configure Mockery in `appsettings.Production.json`:
```json
{
  "MockRepository": {
    "Type": "Git",
    "Git": {
      "RepositoryUrl": "https://github.com/your-org/mockery-mocks.git",
      "Branch": "main",
      "ClonePath": "/app/mocks",
      "AccessToken": ""
    }
  }
}
```

6. Deploy Mockery with `ASPNETCORE_ENVIRONMENT=Production`

### Mode Comparison

| Feature | Local Mode (Development) | Git Mode (Production) |
|---------|-------------------------|----------------------|
| **Environment** | Development | Production/Staging |
| **Configuration** | `appsettings.Development.json` | `appsettings.Production.json` |
| **Mock Location** | Local `mocks/` directory | Git repository |
| **Changes** | Immediate (no restart) | Pulled on startup |
| **Version Control** | Optional (local edits) | Required (Git commits) |
| **Team Sharing** | Manual file sharing | Git push/pull |
| **Use Case** | Local testing, rapid iteration | Production, staging, team collaboration |
