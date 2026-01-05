namespace Mockery.Configuration;

/// <summary>
/// Configuration options for global rate limiting/throttling.
/// Configurable via appsettings.json or environment variables.
/// </summary>
public class ThrottlingOptions
{
    /// <summary>
    /// Section name in configuration.
    /// </summary>
    public const string SectionName = "Throttling";

    /// <summary>
    /// Enable or disable throttling. Default is false.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Maximum requests per second allowed. Default is 100.
    /// </summary>
    public int RequestsPerSecond { get; set; } = 100;

    /// <summary>
    /// Maximum burst size (token bucket capacity). Default is 50.
    /// Allows short bursts of traffic above the steady-state rate.
    /// </summary>
    public int BurstSize { get; set; } = 50;

    /// <summary>
    /// Paths that are excluded from throttling.
    /// Default excludes health check and metrics endpoints.
    /// </summary>
    public string[] ExcludedPaths { get; set; } = 
    [
        "/health/live",
        "/health/ready", 
        "/health/startup",
        "/metrics"
    ];
}
