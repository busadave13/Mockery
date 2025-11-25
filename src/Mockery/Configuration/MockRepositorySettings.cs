namespace Mockery.Configuration;

public class MockRepositorySettings
{
    public string Type { get; set; } = "Git"; // Default to Git for backward compatibility
    public string LocalPath { get; set; } = "./mocks";

    // Git-specific settings (used when Type = "Git")
    public GitSettings Git { get; set; } = new GitSettings();
}

public class GitSettings
{
    public string RepositoryUrl { get; set; } = string.Empty;
    public string Branch { get; set; } = "main";
    public string ClonePath { get; set; } = "/app/mocks";
    public string AccessToken { get; set; } = string.Empty;
    public AutoRefreshSettings AutoRefresh { get; set; } = new AutoRefreshSettings();
}

public class AutoRefreshSettings
{
    public bool Enabled { get; set; } = true;
    public int IntervalMinutes { get; set; } = 5;
    
    /// <summary>
    /// Interval in seconds for more granular control. If set to a value > 0, this takes precedence over IntervalMinutes.
    /// Primarily useful for testing.
    /// </summary>
    public int IntervalSeconds { get; set; } = 0;
    
    /// <summary>
    /// Gets the effective interval as a TimeSpan. Uses IntervalSeconds if > 0, otherwise IntervalMinutes.
    /// </summary>
    public TimeSpan GetInterval() => IntervalSeconds > 0 
        ? TimeSpan.FromSeconds(IntervalSeconds) 
        : TimeSpan.FromMinutes(IntervalMinutes);
}
