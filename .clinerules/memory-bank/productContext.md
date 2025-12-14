# Product Context - Mockery

## Purpose

Mockery is a REST API service for serving HTTP mock responses. It enables development and testing teams to:

- Test microservices in isolation
- Simulate third-party API responses
- Create predictable test environments
- Support contract testing across distributed systems

## Target Users

1. **Development Teams** - Primary users consuming mocks for testing
2. **QA Engineers** - Using mocks for integration and E2E testing
3. **DevOps Engineers** - Managing deployment and infrastructure
4. **Technical Leads** - Reviewing and approving mock changes via Git workflows

## Core Value Proposition

- **Dual Storage Modes**: Local file system for development, Git repository for production
- **Simple API**: Single GET endpoint with header-based mock selection (`X-Mock-ID`)
- **Version Control**: Full audit trail of mock changes via Git history
- **Zero Setup**: No tokens, authentication, or complex configuration required
- **Observable**: Built-in OpenTelemetry integration for monitoring

## Key Features

| Feature | Description |
|---------|-------------|
| **Local Mode** | Direct file system access for rapid local development |
| **Git Mode** | Repository-based storage with full version control |
| **Random Selection** | Support for multiple mock IDs with random selection |
| **Custom Headers** | Optional `.headers.json` files for custom HTTP response headers |
| **Status Code Control** | Dynamic status code behavior via `.status.json` files |
| **Content-Type Detection** | Automatic detection from file extension |
| **Health Checks** | Kubernetes-compatible liveness, readiness, and startup probes |
| **OpenTelemetry** | Built-in logs, metrics, and traces |

## User Workflow

### Development (Local Mode)
1. Create mock files in `mocks/{ServiceName}/{FileId}.{extension}`
2. Run `dotnet run` from `src/Mockery`
3. Request mocks via `curl -H "X-Mock-ID: ServiceName/FileId" http://localhost:8080/api/mock`
4. Changes are picked up immediately (no restart needed)

### Production (Git Mode)
1. Add mock files to Git repository
2. Create pull request for review
3. Merge to main branch
4. Service automatically refreshes from Git

## Business Impact

- **Developer Productivity**: Standard Git workflows for mock management
- **Simplicity**: Single API endpoint reduces cognitive overhead
- **Collaboration**: Pull request reviews for mock changes
- **Cost Efficiency**: No database infrastructure required
- **Observability**: Built-in telemetry for monitoring and debugging
