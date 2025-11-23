# C# Coding Standards & Best Practices

This document outlines the coding standards and best practices for C# development in this project. These guidelines ensure code consistency, maintainability, and quality across the codebase.

## Table of Contents

- [General Principles](#general-principles)
- [Naming Conventions](#naming-conventions)
- [Code Organization](#code-organization)
- [Language Features](#language-features)
- [Error Handling](#error-handling)
- [Documentation](#documentation)
- [Testing Standards](#testing-standards)
- [Performance Guidelines](#performance-guidelines)
- [Security Considerations](#security-considerations)

## General Principles

### Code Quality Fundamentals

1. **Readability First**: Code is read more often than written
2. **Single Responsibility**: Each class/method should have one clear purpose
3. **DRY (Don't Repeat Yourself)**: Avoid code duplication
4. **KISS (Keep It Simple, Stupid)**: Prefer simple solutions over complex ones
5. **Fail Fast**: Validate inputs early and provide clear error messages

### Modern C# Features

- Use **nullable reference types** (`#nullable enable`)
- Prefer **implicit usings** for common namespaces
- Use **file-scoped namespaces** when possible
- Leverage **pattern matching** and **switch expressions**
- Use **records** for immutable data structures

## Naming Conventions

### Casing Rules

| Element | Casing | Example |
|---------|--------|---------|
| Namespace | PascalCase | `MyCompany.Application` |
| Class/Interface/Record | PascalCase | `UserAccount`, `IRepository` |
| Method/Property | PascalCase | `GetUsers()`, `EmailAddress` |
| Local Variables | camelCase | `userAccount` |
| Private Fields | camelCase with _ | `_logger`, `_repository` |
| Constants | PascalCase | `DefaultTimeout` |
| Enum Values | PascalCase | `LogLevel.Information` |
| Parameters | camelCase | `userId`, `emailAddress` |

### Specific Guidelines

```csharp
// ✅ Good: Clear, descriptive names
public class DatabaseConnectionManager
{
    private readonly ILogger _logger;
    private const int DefaultTimeoutSeconds = 30;
    
    public IList<DatabaseConnection> GetConnectionsForEnvironment(string environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environment, nameof(environment));
        // ...
    }
}

// ❌ Bad: Abbreviated, unclear names
public class DbMgr
{
    private ILogger l;
    private const int TO = 30;
    
    public IList<DatabaseConnection> GetConns(string env)
    {
        // ...
    }
}
```

### Interface Naming

- Prefix interfaces with `I`: `IRepository`, `IEmailService`
- Use descriptive names that clearly indicate the contract

```csharp
// ✅ Good
public interface IEmailService : IDisposable
{
    string Name { get; }
    Task SendEmailAsync(EmailMessage message);
}

// ❌ Bad
public interface EmailServiceInterface
{
    string N { get; }
    Task SendAsync(object msg);
}
```

## Code Organization

### File Structure

```csharp
// File header (if required)
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;

namespace MyCompany.Application.Core;

/// <summary>
/// Represents server configuration for different environments.
/// </summary>
/// <param name="environment">The deployment environment name.</param>
/// <param name="apiEndpoint">The API endpoint URL.</param>
/// <param name="databaseConnectionString">The database connection string.</param>
/// <param name="cacheEndpoint">The cache endpoint URL.</param>
public class ServerConfiguration(
    string environment, 
    string apiEndpoint, 
    string databaseConnectionString, 
    string cacheEndpoint)
{
    public string Environment { get; } = environment;
    public string ApiEndpoint { get; } = apiEndpoint;
    public string DatabaseConnectionString { get; } = databaseConnectionString;
    public string CacheEndpoint { get; } = cacheEndpoint;
}
```

### Class Organization Order

1. Constants
2. Static fields
3. Private fields
4. Constructors
5. Properties
6. Public methods
7. Internal methods
8. Private methods
9. Nested classes/enums

```csharp
public class ExampleClass
{
    // Constants
    private const int MaxRetryAttempts = 3;
    
    // Static fields
    private static readonly ILogger Logger = LoggerFactory.Create();
    
    // Private fields
    private readonly IServiceProvider _serviceProvider;
    private readonly List<string> _endpoints;
    
    // Constructor
    public ExampleClass(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _endpoints = new List<string>();
    }
    
    // Properties
    public int RetryCount { get; private set; }
    
    // Public methods
    public void PublicMethod() { }
    
    // Private methods
    private void PrivateMethod() { }
}
```

## Language Features

### Nullable Reference Types

Always enable nullable reference types and handle nullability explicitly:

```csharp
#nullable enable

public class ConfigurationService
{
    private readonly IConfiguration _configuration;
    
    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }
    
    public string? GetConnectionString(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        return _configuration.GetConnectionString(name);
    }
    
    public string GetRequiredConnectionString(string name)
    {
        var connectionString = GetConnectionString(name);
        return connectionString ?? throw new InvalidOperationException($"Connection string '{name}' not found.");
    }
}
```

### Pattern Matching & Switch Expressions

Use modern pattern matching features:

```csharp
// ✅ Good: Switch expression
public static string GetEndpointType(string environment) => environment.ToLowerInvariant() switch
{
    "development" => "Local",
    "staging" => "Cloud",
    "production" => "Cloud",
    _ => "Unknown"
};

// ✅ Good: Pattern matching
public static bool IsValidEndpoint(object endpoint) => endpoint switch
{
    string url when Uri.TryCreate(url, UriKind.Absolute, out _) => true,
    Uri uri when uri.IsAbsoluteUri => true,
    _ => false
};

// ❌ Bad: Traditional if-else chains
public static string GetEndpointType(string environment)
{
    if (environment.ToLowerInvariant() == "development")
        return "Local";
    else if (environment.ToLowerInvariant() == "staging")
        return "Cloud";
    else if (environment.ToLowerInvariant() == "production")
        return "Cloud";
    else
        return "Unknown";
}
```

### Records and Primary Constructors

Use records for immutable data and primary constructors for simple classes:

```csharp
// ✅ Good: Record for immutable data
public record ApplicationConfiguration(
    string ServiceName,
    string ServiceVersion,
    Dictionary<string, string> Settings);

// ✅ Good: Primary constructor
public class DataProcessor(IRepository repository, ILogger<DataProcessor> logger)
{
    private readonly IRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    
    public void ProcessData(string name, double value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        // Implementation
    }
}
```

### Collection Expressions

Use modern collection syntax when available:

```csharp
// ✅ Good: Collection expressions (.NET 8+)
private static readonly string[] SupportedFormats = ["json", "xml", "yaml"];
private static readonly Dictionary<string, int> DefaultPorts = new()
{
    ["http"] = 80,
    ["https"] = 443,
    ["grpc"] = 5000
};

// ✅ Good: Collection initialization
public IList<string> GetSupportedFormats() => ["json", "xml", "yaml"];
```

## Error Handling

### Argument Validation

Always validate public method arguments:

```csharp
public class EndpointValidator
{
    public bool ValidateEndpoint(string endpoint, TimeSpan timeout)
    {
        // Use ArgumentException.ThrowIfNullOrWhiteSpace for strings
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint, nameof(endpoint));
        
        // Use ArgumentOutOfRangeException for value types
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout.TotalSeconds, 0, nameof(timeout));
        
        // Custom validation with clear messages
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid URL format: '{endpoint}'", nameof(endpoint));
        }
        
        return ValidateUri(uri);
    }
    
    private static bool ValidateUri(Uri uri)
    {
        // Private methods can assume inputs are already validated
        return uri.Scheme is "http" or "https";
    }
}
```

### Exception Handling

```csharp
// ✅ Good: Specific exception handling with logging
public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
{
    const int maxAttempts = 3;
    
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (attempt < maxAttempts)
        {
            _logger.LogWarning(ex, "Request failed on attempt {Attempt}/{MaxAttempts}. Retrying...", 
                attempt, maxAttempts);
            
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Operation failed permanently on attempt {Attempt}", attempt);
            throw;
        }
    }
    
    throw new InvalidOperationException("This should never be reached");
}

// ❌ Bad: Catching all exceptions
public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation)
{
    try
    {
        return await operation();
    }
    catch (Exception)
    {
        // No logging, unclear what went wrong
        throw;
    }
}
```

### Custom Exceptions

Create specific exception types for domain-specific errors:

```csharp
public class ConfigurationException : Exception
{
    public ConfigurationException(string message) : base(message) { }
    
    public ConfigurationException(string message, Exception innerException) 
        : base(message, innerException) { }
    
    public static ConfigurationException InvalidEndpoint(string endpoint) =>
        new($"Invalid endpoint: '{endpoint}'. Must be a valid HTTP or HTTPS URL.");
    
    public static ConfigurationException MissingConfiguration(string key) =>
        new($"Required configuration key '{key}' is missing or empty.");
}
```

## Documentation

### XML Documentation

Provide comprehensive XML documentation for all public APIs:

```csharp
/// <summary>
/// Configures application services for ASP.NET Core applications.
/// </summary>
/// <param name="builder">The <see cref="WebApplicationBuilder"/> to configure.</param>
/// <param name="services">Optional collection of custom services to register.</param>
/// <param name="configurations">Optional collection of custom configurations to apply.</param>
/// <returns>The same <see cref="WebApplicationBuilder"/> instance for method chaining.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
/// <example>
/// <code>
/// var builder = WebApplication.CreateBuilder(args);
/// builder.AddApplicationServices(
///     services: new[] { new MyService() },
///     configurations: new[] { new MyConfiguration() }
/// );
/// 
/// var app = builder.Build();
/// app.UseApplicationServices();
/// app.Run();
/// </code>
/// </example>
public static WebApplicationBuilder AddApplicationServices(
    this WebApplicationBuilder builder,
    IEnumerable<IService>? services = null,
    IEnumerable<IConfiguration>? configurations = null)
{
    ArgumentNullException.ThrowIfNull(builder, nameof(builder));
    // Implementation...
}
```

### Code Comments

Use comments sparingly but effectively:

```csharp
public class EndpointConfiguration
{
    // Use comments to explain WHY, not WHAT
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    
    public bool ValidateEndpoint(string endpoint)
    {
        // Explain business logic or non-obvious decisions
        // We allow both HTTP and HTTPS for flexibility in different environments
        var uri = new Uri(endpoint);
        return uri.Scheme is "http" or "https";
    }
    
    private void ConfigureEndpoints()
    {
        // TODO: Add support for custom port configuration
        // HACK: Temporary workaround for legacy systems - remove in v2.0
        // NOTE: This assumes all endpoints use standard ports
    }
}
```

## Testing Standards

### Test Organization

Follow the Arrange-Act-Assert pattern:

```csharp
public class EndpointValidatorTests
{
    [Fact]
    public void ValidateEndpoint_WithValidHttpsUrl_ReturnsTrue()
    {
        // Arrange
        var validator = new EndpointValidator();
        const string validEndpoint = "https://api.example.com/telemetry";
        
        // Act
        var result = validator.ValidateEndpoint(validEndpoint);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("not-a-url")]
    [InlineData("ftp://invalid-scheme.com")]
    public void ValidateEndpoint_WithInvalidInput_ThrowsArgumentException(string invalidEndpoint)
    {
        // Arrange
        var validator = new EndpointValidator();
        
        // Act & Assert
        var action = () => validator.ValidateEndpoint(invalidEndpoint);
        action.Should().Throw<ArgumentException>();
    }
}
```

### Test Naming

Use descriptive test names that clearly indicate:
- The method being tested
- The scenario/input
- The expected outcome

```csharp
// ✅ Good: Clear test names
[Fact]
public void GetEndpoints_DevelopmentEnvironment_ReturnsDevelopmentEndpoint() { }

[Fact]
public void AddApplicationServices_WithNullBuilder_ThrowsArgumentNullException() { }

[Fact]
public void ConfigureMetrics_WithCustomMeter_RegistersMetricsCorrectly() { }

// ❌ Bad: Unclear test names
[Fact]
public void Test1() { }

[Fact]
public void TestGetEndpoints() { }

[Fact]
public void ValidInput() { }
```

### Mock Usage

Use mocks appropriately for external dependencies:

```csharp
public class DataServiceTests
{
    [Fact]
    public void ProcessData_WithValidInput_CallsLoggerCorrectly()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DataService>>();
        var service = new DataService(mockLogger.Object);
        var data = new ProcessedData("test-data", 42.0);
        
        // Act
        service.ProcessData(data);
        
        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("test-data")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

## Performance Guidelines

### Asynchronous Programming

```csharp
// ✅ Good: Proper async/await usage
public async Task<IEnumerable<Endpoint>> GetEndpointsAsync(CancellationToken cancellationToken = default)
{
    var endpoints = new List<Endpoint>();
    
    await foreach (var endpoint in DiscoverEndpointsAsync(cancellationToken).ConfigureAwait(false))
    {
        if (await ValidateEndpointAsync(endpoint, cancellationToken).ConfigureAwait(false))
        {
            endpoints.Add(endpoint);
        }
    }
    
    return endpoints;
}

// ✅ Good: ConfigureAwait(false) in library code
private async Task<bool> ValidateEndpointAsync(string endpoint, CancellationToken cancellationToken)
{
    using var client = new HttpClient();
    try
    {
        var response = await client.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
    catch (HttpRequestException)
    {
        return false;
    }
}

// ❌ Bad: Blocking async calls
public IEnumerable<Endpoint> GetEndpoints()
{
    return GetEndpointsAsync().Result; // This can cause deadlocks
}
```

### Memory Management

```csharp
// ✅ Good: Proper disposal patterns
public class ResourceManager : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly FileStream _fileStream;
    private bool _disposed;
    
    public ResourceManager(string filePath)
    {
        _httpClient = new HttpClient();
        _fileStream = File.OpenWrite(filePath);
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _httpClient?.Dispose();
            _fileStream?.Dispose();
            _disposed = true;
        }
    }
}

// ✅ Good: Use using statements for disposable resources
public async Task ProcessFileAsync(string filePath)
{
    using var fileStream = File.OpenRead(filePath);
    using var reader = new StreamReader(fileStream);
    
    var content = await reader.ReadToEndAsync().ConfigureAwait(false);
    ProcessContent(content);
}
```

### Collection Performance

```csharp
// ✅ Good: Use appropriate collection types
public class EndpointCache
{
    // Use HashSet for uniqueness and fast lookups
    private readonly HashSet<string> _knownEndpoints = new(StringComparer.OrdinalIgnoreCase);
    
    // Use Dictionary for key-value lookups
    private readonly Dictionary<string, DateTime> _endpointLastSeen = new(StringComparer.OrdinalIgnoreCase);
    
    // Use List for ordered collections with known size
    public IList<string> GetRecentEndpoints(int count)
    {
        var recentEndpoints = new List<string>(count);
        // Implementation...
        return recentEndpoints;
    }
}

// ❌ Bad: Using wrong collection types
public class BadEndpointCache
{
    private readonly List<string> _knownEndpoints = new(); // Slow for Contains() operations
    private readonly List<KeyValuePair<string, DateTime>> _endpointLastSeen = new(); // Slow for lookups
}
```

## Security Considerations

### Input Validation

```csharp
public class SecurityAwareService
{
    public void ProcessUserInput(string userInput, string filePath)
    {
        // Validate and sanitize all inputs
        ArgumentException.ThrowIfNullOrWhiteSpace(userInput, nameof(userInput));
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        
        // Prevent path traversal attacks
        var fullPath = Path.GetFullPath(filePath);
        var allowedDirectory = Path.GetFullPath("./allowed/");
        if (!fullPath.StartsWith(allowedDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access to the specified path is not allowed.");
        }
        
        // Validate input length to prevent DoS
        if (userInput.Length > 1000)
        {
            throw new ArgumentException("Input too long", nameof(userInput));
        }
        
        ProcessSafeInput(userInput, fullPath);
    }
    
    private static void ProcessSafeInput(string input, string path)
    {
        // Process validated input
    }
}
```

### Secrets and Configuration

```csharp
// ✅ Good: Never hardcode secrets
public class ConfigurationService
{
    private readonly IConfiguration _configuration;
    
    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public string GetApiKey()
    {
        // Use configuration providers for secrets
        var apiKey = _configuration["ApiKey"] ?? 
                    Environment.GetEnvironmentVariable("API_KEY");
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("API key not configured");
        }
        
        return apiKey;
    }
}

// ❌ Bad: Hardcoded secrets
public class BadConfigurationService
{
    private const string ApiKey = "super-secret-key-123"; // Never do this!
}
```

---

## Enforcement

These standards should be enforced through:

1. **Code Reviews**: All code changes must be reviewed for adherence
2. **Static Analysis**: Use tools like SonarQube, CodeQL, or Roslyn analyzers
3. **CI/CD Pipeline**: Automated checks for code quality and standards
4. **Team Training**: Regular training sessions on best practices
5. **Documentation Updates**: Keep this document current with evolving practices

### Required Build and Test Verification

All code changes **MUST** be verified through the following mandatory steps before completion:

#### Pre-Commit Requirements
- **Build Verification**: Code must compile successfully without warnings
- **Unit Test Execution**: All existing unit tests must pass
- **New Test Requirements**: New functionality must include corresponding unit tests
- **Integration Test Verification**: Integration tests must pass if applicable

#### Mandatory Commands
Execute the following commands in sequence to verify code quality:

```bash
# Build the solution and ensure no compilation errors
dotnet build --configuration Release --no-restore

# Run all unit tests with coverage
dotnet test --configuration Release --no-build --logger trx --collect:"XPlat Code Coverage"

# Check for code style violations (if using .editorconfig)
dotnet format --verify-no-changes --verbosity diagnostic

# Security vulnerability scan (if using security analyzers)
dotnet list package --vulnerable --include-transitive
```

#### Failure Response Protocol
If any of the above steps fail:

1. **Do not proceed with commit/merge**
2. **Fix all compilation errors immediately**
3. **Resolve failing tests before continuing**
4. **Add missing tests for new functionality**
5. **Address security vulnerabilities**
6. **Re-run verification steps until all pass**

#### Continuous Integration Gates
The CI/CD pipeline must include:

- **Automated Build Gate**: Builds must succeed on all target platforms
- **Test Gate**: All tests must pass with minimum coverage thresholds
- **Quality Gate**: Static analysis must meet defined quality criteria
- **Security Gate**: No high/critical security vulnerabilities allowed

#### Developer Responsibilities
Every developer must:

- Run `dotnet build` locally before committing
- Execute `dotnet test` to verify all tests pass
- Ensure code coverage meets project standards (typically ≥80%)
- Verify no new analyzer warnings are introduced
- Test changes in realistic scenarios before submission

**Violation of these build and test requirements will result in immediate rejection of code changes until compliance is achieved.**

## References

- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- [.NET Design Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [C# Language Specification](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/)
- [Async Best Practices](https://docs.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
