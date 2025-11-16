namespace Mockery.Configuration;

public class MockRepositorySettings
{
    public string Type { get; set; } = "Git"; // Default to Git for backward compatibility
    public string LocalPath { get; set; } = "./mocks";
}
