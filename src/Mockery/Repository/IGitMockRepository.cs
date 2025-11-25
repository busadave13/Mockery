namespace Mockery.Repository;

public interface IGitMockRepository
{
    Task InitializeAsync();
    Task<(string Content, string Extension)?> FindMockFileAsync(string serviceName, string fileId);
    Task<Dictionary<string, string>?> FindHeadersFileAsync(string serviceName, string fileId);
    
    /// <summary>
    /// Finds a status file (e.g., 504.status.json) for the given service and file ID.
    /// Returns the status code from the filename and optional content from the file.
    /// </summary>
    Task<(int StatusCode, string? Content)?> FindStatusFileAsync(string serviceName, string fileId);
    
    Task RefreshAsync();
}
