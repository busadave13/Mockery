using Mockery.BusinessLogic;
using Mockery.Configuration;
using Mockery.Middleware;
using Mockery.Repository;
using Mockery.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configure Git repository options from environment variables
builder.Services.Configure<GitRepositoryOptions>(options =>
{
    options.RepositoryUrl = Environment.GetEnvironmentVariable("GIT_REPOSITORY_URL") ?? "";
    options.Branch = Environment.GetEnvironmentVariable("GIT_BRANCH") ?? "main";
    options.ClonePath = Environment.GetEnvironmentVariable("GIT_CLONE_PATH") ?? "/app/mocks";
    options.AccessToken = Environment.GetEnvironmentVariable("GIT_ACCESS_TOKEN") ?? "";
});

// Configure rate limiting from appsettings
builder.Services.Configure<RateLimitingOptions>(
    builder.Configuration.GetSection(RateLimitingOptions.SectionName));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add application services
builder.Services.AddSingleton<IGitMockRepository, GitMockRepository>();
builder.Services.AddScoped<IMockService, MockService>();
builder.Services.AddSingleton<IContentTypeResolver, ContentTypeResolver>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("live", () => HealthCheckResult.Healthy("Application is alive"))
    .AddCheck("ready", () =>
    {
        // Check if Git repository is accessible
        var gitOptions = builder.Configuration.GetSection("GitRepository").Get<GitRepositoryOptions>();
        var clonePath = Environment.GetEnvironmentVariable("GIT_CLONE_PATH") ?? "/app/mocks";

        if (Directory.Exists(Path.Combine(clonePath, ".git")))
        {
            return HealthCheckResult.Healthy("Git repository is accessible");
        }

        return HealthCheckResult.Unhealthy("Git repository not accessible");
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

// Initialize Git repository on startup
var logger = app.Services.GetRequiredService<ILogger<Program>>();
try
{
    logger.LogInformation("Initializing Git repository...");
    var repository = app.Services.GetRequiredService<IGitMockRepository>();
    await repository.InitializeAsync();
    logger.LogInformation("Git repository initialized successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to initialize Git repository");
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
