using Mockery.BusinessLogic;
using Mockery.Configuration;
using Mockery.Middleware;
using Mockery.Repository;
using Mockery.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Read repository configuration from appsettings
var mockRepoSettings = builder.Configuration.GetSection("MockRepository").Get<MockRepositorySettings>()
    ?? new MockRepositorySettings();

// Configure repository options based on repository type
if (mockRepoSettings.Type.Equals("Local", StringComparison.OrdinalIgnoreCase))
{
    // Local development mode - use local file system
    builder.Services.Configure<GitRepositoryOptions>(options =>
    {
        options.ClonePath = mockRepoSettings.LocalPath;
        options.RepositoryUrl = ""; // Not used in local mode
        options.Branch = "main";
        options.AccessToken = "";
    });
}
else
{
    // Git mode - use appsettings configuration only
    builder.Services.Configure<GitRepositoryOptions>(options =>
    {
        options.RepositoryUrl = mockRepoSettings.Git.RepositoryUrl;
        options.Branch = mockRepoSettings.Git.Branch;
        options.ClonePath = mockRepoSettings.Git.ClonePath;
        options.AccessToken = mockRepoSettings.Git.AccessToken;
    });
}

// Configure rate limiting from appsettings
builder.Services.Configure<RateLimitingOptions>(
    builder.Configuration.GetSection(RateLimitingOptions.SectionName));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add application services - register appropriate repository based on configuration
if (mockRepoSettings.Type.Equals("Local", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IGitMockRepository, LocalFileMockRepository>();
}
else
{
    builder.Services.AddSingleton<IGitMockRepository, GitMockRepository>();
}
builder.Services.AddScoped<IMockService, MockService>();
builder.Services.AddSingleton<IContentTypeResolver, ContentTypeResolver>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy("Application is alive"))
    .AddCheck("ready", () =>
    {
        // Check if mock repository is accessible (either Git or Local)
        string clonePath;
        bool isLocal = mockRepoSettings.Type.Equals("Local", StringComparison.OrdinalIgnoreCase);

        if (isLocal)
        {
            clonePath = mockRepoSettings.LocalPath;
        }
        else
        {
            clonePath = mockRepoSettings.Git.ClonePath;
        }

        var mocksPath = Path.Combine(clonePath, "mocks");

        // For local mode, check if mocks directory exists
        // For Git mode, check if .git directory exists
        if (isLocal && Directory.Exists(mocksPath))
        {
            return HealthCheckResult.Healthy("Local mock repository is accessible");
        }
        else if (!isLocal && Directory.Exists(Path.Combine(clonePath, ".git")))
        {
            return HealthCheckResult.Healthy("Git repository is accessible");
        }

        return HealthCheckResult.Unhealthy($"Mock repository not accessible (Type: {mockRepoSettings.Type})");
    })
    .AddCheck("startup", () => HealthCheckResult.Healthy("Application startup complete"));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Initialize mock repository on startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
try
{
    var repoType = mockRepoSettings.Type.Equals("Local", StringComparison.OrdinalIgnoreCase) ? "Local" : "Git";
    logger.LogInformation("Initializing {RepositoryType} mock repository...", repoType);
    var repository = app.Services.GetRequiredService<IGitMockRepository>();
    await repository.InitializeAsync();
    logger.LogInformation("{RepositoryType} mock repository initialized successfully", repoType);
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to initialize mock repository");
    // Continue running - health checks will report unhealthy status
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add rate limiting middleware
app.UseMiddleware<RateLimitingMiddleware>();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name == "live"
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name == "ready"
});

app.MapHealthChecks("/health/startup", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name == "startup"
});

logger.LogInformation("Mockery service starting on {Urls}", string.Join(", ", builder.Configuration["ASPNETCORE_URLS"]?.Split(';') ?? new[] { "http://localhost:8080" }));

app.Run();

// Make Program class accessible to tests
public partial class Program { }
