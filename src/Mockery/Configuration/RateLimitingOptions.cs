namespace Mockery.Configuration;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;
    public PerIpOptions PerIp { get; set; } = new();
    public GlobalOptions Global { get; set; } = new();
    public int QueueLimit { get; set; } = 0;
}

public class PerIpOptions
{
    public bool Enabled { get; set; } = true;
    public int PermitLimit { get; set; } = 100;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}

public class GlobalOptions
{
    public bool Enabled { get; set; } = true;
    public int PermitLimit { get; set; } = 1000;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
