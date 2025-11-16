using Microsoft.Extensions.Options;
using Mockery.Configuration;
using System.Collections.Concurrent;

namespace Mockery.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitingOptions _options;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    // Per-IP tracking: IP -> (timestamp, count)
    private static readonly ConcurrentDictionary<string, RequestCounter> _perIpCounters = new();

    // Global tracking
    private static RequestCounter _globalCounter = new();

    public RateLimitingMiddleware(
        RequestDelegate next,
        IOptions<RateLimitingOptions> options,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _options = options.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Check per-IP rate limit
        if (_options.PerIp.Enabled)
        {
            var perIpCounter = _perIpCounters.GetOrAdd(clientIp, _ => new RequestCounter());

            if (!perIpCounter.TryIncrementWithinWindow(now, _options.PerIp.Window, _options.PerIp.PermitLimit))
            {
                _logger.LogWarning("Per-IP rate limit exceeded for {ClientIp}", clientIp);
                await ReturnRateLimitError(context, "per-ip", _options.PerIp.PermitLimit, (int)_options.PerIp.Window.TotalSeconds);
                return;
            }
        }

        // Check global rate limit
        if (_options.Global.Enabled)
        {
            if (!_globalCounter.TryIncrementWithinWindow(now, _options.Global.Window, _options.Global.PermitLimit))
            {
                _logger.LogWarning("Global rate limit exceeded");
                await ReturnRateLimitError(context, "global", _options.Global.PermitLimit, (int)_options.Global.Window.TotalSeconds);
                return;
            }
        }

        await _next(context);
    }

    private async Task ReturnRateLimitError(HttpContext context, string limitType, int limit, int retryAfter)
    {
        context.Response.StatusCode = 429;
        context.Response.ContentType = "application/json";
        context.Response.Headers["Retry-After"] = retryAfter.ToString();

        var errorResponse = new
        {
            error = "Rate limit exceeded",
            limitType,
            message = limitType == "per-ip"
                ? "Too many requests from this IP address. Please try again later."
                : "Service is currently experiencing high load. Please try again later.",
            limit,
            retryAfter
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }
}

public class RequestCounter
{
    private long _count;
    private DateTimeOffset _windowStart;
    private readonly object _lock = new();

    public bool TryIncrementWithinWindow(DateTimeOffset now, TimeSpan window, int permitLimit)
    {
        lock (_lock)
        {
            // Check if we're in a new window
            if (now - _windowStart >= window)
            {
                _windowStart = now;
                _count = 0;
            }

            // Check if we're within limit
            if (_count >= permitLimit)
            {
                return false;
            }

            _count++;
            return true;
        }
    }
}
