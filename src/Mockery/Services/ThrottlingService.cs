using Mockery.Configuration;
using Microsoft.Extensions.Options;

namespace Mockery.Services;

/// <summary>
/// Token bucket implementation for global rate limiting.
/// Thread-safe singleton service that manages request throttling.
/// </summary>
public class ThrottlingService : IThrottlingService
{
    private readonly IOptionsMonitor<ThrottlingOptions> _optionsMonitor;
    private readonly ILogger<ThrottlingService> _logger;
    private readonly object _lock = new();
    
    private double _tokens;
    private DateTime _lastRefill;

    public ThrottlingService(
        IOptionsMonitor<ThrottlingOptions> optionsMonitor,
        ILogger<ThrottlingService> logger)
    {
        _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize bucket to full capacity
        var options = _optionsMonitor.CurrentValue;
        _tokens = options.BurstSize;
        _lastRefill = DateTime.UtcNow;
        
        _logger.LogInformation(
            "Throttling service initialized. Enabled: {Enabled}, Rate: {Rate}/s, Burst: {Burst}",
            options.Enabled,
            options.RequestsPerSecond,
            options.BurstSize);
    }

    /// <inheritdoc />
    public bool IsEnabled => _optionsMonitor.CurrentValue.Enabled;

    /// <inheritdoc />
    public int AvailableTokens
    {
        get
        {
            lock (_lock)
            {
                RefillTokens();
                return (int)Math.Floor(_tokens);
            }
        }
    }

    /// <inheritdoc />
    public ThrottleResult TryConsume()
    {
        var options = _optionsMonitor.CurrentValue;
        
        // If throttling is disabled, always allow
        if (!options.Enabled)
        {
            return new ThrottleResult
            {
                IsAllowed = true,
                RemainingTokens = options.BurstSize,
                Limit = options.RequestsPerSecond,
                RetryAfterSeconds = 0
            };
        }

        lock (_lock)
        {
            RefillTokens();

            if (_tokens >= 1)
            {
                _tokens -= 1;
                return new ThrottleResult
                {
                    IsAllowed = true,
                    RemainingTokens = (int)Math.Floor(_tokens),
                    Limit = options.RequestsPerSecond,
                    RetryAfterSeconds = 0
                };
            }

            // Calculate time until next token is available
            double tokensNeeded = 1 - _tokens;
            double secondsUntilToken = tokensNeeded / options.RequestsPerSecond;

            _logger.LogDebug(
                "Request throttled. Available tokens: {Tokens:F2}, Retry after: {RetryAfter:F2}s",
                _tokens,
                secondsUntilToken);

            return new ThrottleResult
            {
                IsAllowed = false,
                RemainingTokens = 0,
                Limit = options.RequestsPerSecond,
                RetryAfterSeconds = Math.Ceiling(secondsUntilToken)
            };
        }
    }

    /// <summary>
    /// Refills tokens based on elapsed time since last refill.
    /// Must be called within lock.
    /// </summary>
    private void RefillTokens()
    {
        var options = _optionsMonitor.CurrentValue;
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;
        
        if (elapsed > 0)
        {
            // Add tokens based on elapsed time and rate
            double tokensToAdd = elapsed * options.RequestsPerSecond;
            _tokens = Math.Min(options.BurstSize, _tokens + tokensToAdd);
            _lastRefill = now;
        }
    }
}
