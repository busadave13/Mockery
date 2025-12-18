namespace Mockery.Models;

/// <summary>
/// Response model for deleting a mock file.
/// </summary>
public record DeleteMockResponse
{
    /// <summary>
    /// The full path of the deleted file (e.g., "weather/prod/success.json").
    /// </summary>
    public string DeletedFile { get; init; } = string.Empty;
    
    /// <summary>
    /// List of folders that were deleted because they became empty after file deletion.
    /// </summary>
    public List<string> DeletedFolders { get; init; } = new();
    
    /// <summary>
    /// Indicates whether the deletion was committed and pushed to Git (true for Git mode).
    /// </summary>
    public bool CommittedToGit { get; init; }
}
