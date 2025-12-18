namespace Mockery.Models;

/// <summary>
/// Response model for creating a mock file.
/// </summary>
public record CreateMockResponse
{
    /// <summary>
    /// The directory path where the file was created (e.g., "weather/prod").
    /// </summary>
    public string Path { get; init; } = string.Empty;
    
    /// <summary>
    /// The name of the created file (e.g., "success.json").
    /// </summary>
    public string FileName { get; init; } = string.Empty;
    
    /// <summary>
    /// Size of the created file in bytes.
    /// </summary>
    public long Size { get; init; }
    
    /// <summary>
    /// Indicates whether the file was committed and pushed to Git (true for Git mode).
    /// </summary>
    public bool CommittedToGit { get; init; }
}
