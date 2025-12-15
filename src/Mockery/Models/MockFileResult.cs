namespace Mockery.Models;

public class MockFileResult
{
    /// <summary>
    /// The mock ID that was served (e.g., "FooBar/1234")
    /// </summary>
    public string MockId { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
    public bool ShouldReturnContent { get; set; } = true;

    /// <summary>
    /// Status code derived from a .status.json file (e.g., 504 from 504.status.json)
    /// </summary>
    public int? StatusCode { get; set; }
}
