using Mockery.Configuration;
using Mockery.Services;
using Microsoft.Extensions.Options;

namespace Mockery.Middleware;

/// <summary>
/// ASP.NET Core middleware for global request throttling.
/// Uses token bucket algorithm to limit request rate.
/// </summary>
public class ThrottlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IThrottlingService _throttlingService;
    private readonly IOptionsMonitor<ThrottlingOptions> _optionsMonitor;
    private readonly ILogger<ThrottlingMiddleware> _logger;
    private readonly MockeryMetrics _metrics;

    public ThrottlingMiddleware(
        RequestDelegate next,
        IThrottlingService throttlingService,
        IOptionsMonitor<ThrottlingOptions> optionsMonitor,
        ILogger<ThrottlingMiddleware> logger,
        MockeryMetrics metrics)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _throttlingService = throttlingService ?? throw new ArgumentNullException(nameof(throttlingService));
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var options = _optionsMonitor.CurrentValue;

        // Skip throttling if disabled
        if (!options.Enabled)
        {
            await _next(context);
            return;
        }

        // Check if path is excluded
        var path = context.Request.Path.Value ?? string.Empty;
        if (IsPathExcluded(path, options.ExcludedPaths))
        {
            await _next(context);
            return;
        }

        // Attempt to consume a token
        var result = _throttlingService.TryConsume();

        // Always add rate limit headers
        context.Response.Headers["X-RateLimit-Limit"] = result.Limit.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = result.RemainingTokens.ToString();

        if (result.IsAllowed)
        {
            await _next(context);
        }
        else
        {
            // Request is throttled
            _metrics.IncrementThrottledRequests();
            
            _logger.LogWarning(
                "Request throttled. Path: {Path}, Limit: {Limit}/s, RetryAfter: {RetryAfter}s",
                path,
                result.Limit,
                result.RetryAfterSeconds);

            context.Response.Headers["Retry-After"] = ((int)result.RetryAfterSeconds).ToString();
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        }
    }

    /// <summary>
    /// Checks if the given path should be excluded from throttling.
    /// </summary>
    private static bool IsPathExcluded(string path, string[] excludedPaths)
    {
        foreach (var excluded in excludedPaths)
        {
            if (path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Extension methods for registering throttling middleware.
/// </summary>
public static class ThrottlingMiddlewareExtensions
{
    /// <summary>
    /// Adds the throttling middleware to the application pipeline.
    /// Should be added early in the pipeline, before routing.
    /// </summary>
    public static IApplicationBuilder UseThrottling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ThrottlingMiddleware>();
    }
}
