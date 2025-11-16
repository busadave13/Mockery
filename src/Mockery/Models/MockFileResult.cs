namespace Mockery.Models;

public class MockFileResult
{
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
    public bool ShouldReturnContent { get; set; } = true;
}
