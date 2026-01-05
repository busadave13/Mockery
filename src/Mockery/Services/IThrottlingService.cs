namespace Mockery.Services;

/// <summary>
/// Result of a throttle check operation.
/// </summary>
public record ThrottleResult
{
    /// <summary>
    /// Whether the request is allowed.
    /// </summary>
    public bool IsAllowed { get; init; }

    /// <summary>
    /// Number of tokens remaining in the bucket.
    /// </summary>
    public int RemainingTokens { get; init; }

    /// <summary>
    /// Configured limit (requests per second).
    /// </summary>
    public int Limit { get; init; }

    /// <summary>
    /// Seconds until next token is available (only set when throttled).
    /// </summary>
    public double RetryAfterSeconds { get; init; }
}

/// <summary>
/// Service interface for global rate limiting using token bucket algorithm.
/// </summary>
public interface IThrottlingService
{
    /// <summary>
    /// Attempts to consume a token from the bucket.
    /// </summary>
    /// <returns>ThrottleResult indicating if request is allowed and rate limit info.</returns>
    ThrottleResult TryConsume();

    /// <summary>
    /// Gets the current number of available tokens without consuming.
    /// </summary>
    int AvailableTokens { get; }

    /// <summary>
    /// Gets whether throttling is enabled.
    /// </summary>
    bool IsEnabled { get; }
}
