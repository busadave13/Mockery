namespace Mockery.Configuration;

public class GitRepositoryOptions
{
    public string RepositoryUrl { get; set; } = string.Empty;
    public string Branch { get; set; } = "main";
    public string ClonePath { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
}
