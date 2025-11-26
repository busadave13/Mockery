# Product Context: Mockery

## Problem Statement
Developers often need to test applications against APIs that may not be available, are still in development, or have rate limits. Setting up mock servers typically requires significant configuration or running additional services.

## Solution
Mockery provides a lightweight, file-based approach to API mocking where:
- Mock responses are simply files on disk
- File naming conventions control behavior (status codes, headers)
- No database or complex configuration required
- Can be version-controlled alongside application code or in a separate repository

## User Experience Goals

### For Developers
1. **Quick Setup**: Drop mock files into a directory and start serving
2. **Intuitive Conventions**: File names indicate response behavior (e.g., `200.status.json`, `1234.headers.json`)
3. **Flexibility**: Support for JSON, HTML, and other content types
4. **Environment Parity**: Same mock service can run locally or in Kubernetes

### For DevOps/Platform Teams
1. **Easy Deployment**: Docker image and Helm charts ready to use
2. **Observable**: OpenTelemetry integration for monitoring
3. **Configurable**: Settings for Git-based mock repositories, rate limiting

## Mock File Conventions

Based on the `mocks/` directory structure:

### Directory Structure
```
mocks/
├── FooBar/           # Mock collection for "FooBar" endpoint
│   ├── 200.status.json      # Response with 200 status
│   ├── 504.status.json      # Response with 504 status
│   ├── 1234.headers.json    # Custom headers for request ID 1234
│   ├── 1234.json            # Response body for request ID 1234
│   └── 5678.html            # HTML response for request ID 5678
└── Products/         # Mock collection for "Products" endpoint
    ├── error.json           # Error response
    └── hydrate.json         # Hydrated product data
```

### File Naming Patterns
- `{id}.json` - JSON response for specific ID
- `{id}.html` - HTML response for specific ID
- `{statusCode}.status.json` - Response with specific HTTP status
- `{id}.headers.json` - Custom headers for specific ID

## Target Users
1. **Frontend Developers** - Testing UI against stable mock data
2. **Backend Developers** - Integration testing with predictable responses
3. **QA Engineers** - Automated testing with controlled responses
4. **DevOps Engineers** - Setting up test environments
