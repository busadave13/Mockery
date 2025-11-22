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
}
