# Mock Files Directory

This directory contains sample mock files for local development and testing.

## Directory Structure

```
mocks/
├── {ServiceName}/
│   ├── {FileId}.{extension}         # Mock content file (e.g., .json, .html, .xml)
│   └── {FileId}.headers.json        # Optional custom headers file
```

## Mock ID Format

Mock IDs follow the pattern: `{ServiceName}/{FileId}`

- **ServiceName**: Must match the folder name exactly (case-sensitive)
- **FileId**: File name without extension
- **Extension**: Determines the Content-Type header

## Example Mock Files

### FooBar Service Examples

- **FooBar/1234** - JSON response with custom headers
  - `FooBar/1234.json` - Simple success response
  - `FooBar/1234.headers.json` - Custom headers (X-Custom-Header, X-Request-ID, etc.)

- **FooBar/5678** - HTML response
  - `FooBar/5678.html` - Sample HTML page

### Products Service Examples

- **Products/hydrate** - Product catalog response
  - `Products/hydrate.json` - List of products with pagination

- **Products/error** - Error response example
  - `Products/error.json` - Error message with details

## Testing Locally

To test these mocks locally:

```bash
# Single mock ID (default 200 OK)
curl -i -H "X-Mock-ID: FooBar/1234" http://localhost:3000/api/mock

# With custom status code
curl -i -H "X-Mock-ID: Products/error" -H "X-Mock-StatusCode: 404" http://localhost:3000/api/mock

# Random selection from multiple IDs
curl -i -H "X-Mock-ID: FooBar/1234,FooBar/5678,Products/hydrate" http://localhost:3000/api/mock
```

## Adding New Mocks

1. Create a service folder if it doesn't exist: `mkdir -p mocks/MyService`
2. Create your mock file: `echo '{"data":"value"}' > mocks/MyService/mockid.json`
3. (Optional) Add custom headers: `echo '{"X-Custom":"Value"}' > mocks/MyService/mockid.headers.json`
4. Restart the application or just make a request (changes are picked up automatically in local mode)

## Supported File Extensions

- `.json` → `application/json`
- `.html` → `text/html`
- `.xml` → `application/xml`
- `.txt` → `text/plain`
- And more (see `ContentTypeResolver` for full list)
