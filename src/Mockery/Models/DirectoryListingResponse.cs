namespace Mockery.Models;

/// <summary>
/// Response model for listing directory contents.
/// </summary>
public record DirectoryListingResponse
{
    /// <summary>
    /// The path that was listed (e.g., "weather/prod" or "/" for root).
    /// </summary>
    public string Path { get; init; } = string.Empty;
    
    /// <summary>
    /// List of items (folders and files) in the directory.
    /// </summary>
    public List<DirectoryItem> Items { get; init; } = new();
}

/// <summary>
/// Represents a single item (file or folder) in a directory listing.
/// </summary>
public record DirectoryItem
{
    /// <summary>
    /// Name of the file or folder.
    /// </summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>
    /// Type of item: "folder" or "file".
    /// </summary>
    public string Type { get; init; } = string.Empty;
    
    /// <summary>
    /// File extension (for files only, e.g., ".json").
    /// </summary>
    public string? Extension { get; init; }
    
    /// <summary>
    /// File size in bytes (for files only).
    /// </summary>
    public long? Size { get; init; }
}
