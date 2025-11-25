namespace Mockery.Models;

public class MockFileResult
{
    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
    public bool ShouldReturnContent { get; set; } = true;
    
    /// <summary>
    /// Status code derived from a .status.json file (e.g., 504 from 504.status.json)
    /// </summary>
    public int? StatusCode { get; set; }
}
