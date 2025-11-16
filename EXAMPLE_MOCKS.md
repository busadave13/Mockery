# Example Mock Repository Structure

This document shows how to set up a separate Git repository containing your mock files for **production/Git mode**.

> **Note for Local Development:** If you're running Mockery locally in development mode, you don't need a separate Git repository. Simply create mock files in the `.mocks/` directory at the project root. See the main [README.md](README.md) for local development setup.

This guide is for teams who want to:
- Deploy Mockery in production/staging with Git-based mock management
- Share mocks across team members via Git workflows
- Version control their mock responses

## Repository Structure

Create a new Git repository with the following structure:

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

## Example Mock Files

### UserService/get-user.json
```json
{
  "id": 12345,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "role": "admin",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

### UserService/get-user.headers.json
```json
{
  "X-User-ID": "12345",
  "X-Request-ID": "abc-123-def",
  "Cache-Control": "max-age=3600"
}
```

### UserService/create-user.json
```json
{
  "id": 12346,
  "name": "Jane Smith",
  "email": "jane.smith@example.com",
  "role": "user",
  "createdAt": "2024-11-15T14:25:00Z",
  "message": "User created successfully"
}
```

### UserService/user-not-found.headers.json
```json
{
  "X-Error-Code": "USER_NOT_FOUND",
  "X-Request-ID": "abc-123-def"
}
```

### ProductService/list-products.json
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

### ProductService/error-response.json
```json
{
  "error": "Internal server error",
  "message": "Database connection failed",
  "code": "DB_ERROR",
  "timestamp": "2024-11-15T14:30:00Z"
}
```

### PaymentService/success.json
```json
{
  "transactionId": "txn_123456789",
  "status": "completed",
  "amount": 99.99,
  "currency": "USD",
  "timestamp": "2024-11-15T14:35:00Z"
}
```

## Usage Examples

Once you've created this repository and configured Mockery to use it, you can make requests like:

### Get User (200 OK)
```bash
curl -i -H "X-Mock-ID: UserService/get-user" http://localhost:3000/api/mock
```

Response:
```json
{
  "id": 12345,
  "name": "John Doe",
  "email": "john.doe@example.com",
  "role": "admin",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

Headers include:
- `Content-Type: application/json`
- `X-User-ID: 12345`
- `X-Request-ID: abc-123-def`
- `Cache-Control: max-age=3600`

### Create User (201 Created)
```bash
curl -i -H "X-Mock-ID: UserService/create-user" \
     -H "X-Mock-StatusCode: 201" \
     http://localhost:3000/api/mock
```

### User Not Found (404)
```bash
curl -i -H "X-Mock-ID: UserService/user-not-found" \
     -H "X-Mock-StatusCode: 404" \
     http://localhost:3000/api/mock
```

Response: Empty body (404 semantics)

Headers include:
- `X-Error-Code: USER_NOT_FOUND`
- `X-Request-ID: abc-123-def`

### Server Error (500)
```bash
curl -i -H "X-Mock-ID: ProductService/error-response" \
     -H "X-Mock-StatusCode: 500" \
     http://localhost:3000/api/mock
```

Response:
```json
{
  "error": "Internal server error",
  "message": "Database connection failed",
  "code": "DB_ERROR",
  "timestamp": "2024-11-15T14:30:00Z"
}
```

### Random Selection
```bash
curl -i -H "X-Mock-ID: PaymentService/success,PaymentService/failed,PaymentService/pending" \
     http://localhost:3000/api/mock
```

This will randomly return one of the three payment responses.

## Setting Up Your Mock Repository

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

3. Add your mock files (as shown above)

4. Commit and push:
```bash
git add .
git commit -m "Initial mock files"
git remote add origin https://github.com/your-org/mockery-mocks.git
git push -u origin main
```

5. Configure Mockery to use your repository:

   **Option A: Via Environment Variables (Docker/Production)**
   ```bash
   export GIT_REPOSITORY_URL="https://github.com/your-org/mockery-mocks.git"
   export GIT_BRANCH="main"
   export GIT_CLONE_PATH="/app/mocks"
   ```

   **Option B: Via Configuration File**

   Update `appsettings.Production.json` (or create it):
   ```json
   {
     "MockRepository": {
       "Type": "Git"
     }
   }
   ```

   Then set the environment variables as above.

6. Run Mockery and start using your mocks!

## Switching Between Local and Git Mode

**For Local Development (Default):**
- Uses `appsettings.Development.json` with `"Type": "Local"`
- Mocks loaded from `.mocks/` directory
- No environment variables needed
- Run with: `dotnet run`

**For Production/Git Mode:**
- Set `"Type": "Git"` in configuration or omit (defaults to Git)
- Set environment variables: `GIT_REPOSITORY_URL`, `GIT_BRANCH`, `GIT_CLONE_PATH`
- Mocks loaded from Git repository
- Run with: `dotnet run --environment Production`
