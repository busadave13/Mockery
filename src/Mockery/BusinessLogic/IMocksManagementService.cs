using Mockery.Models;

namespace Mockery.BusinessLogic;

/// <summary>
/// Service for managing mock files (list, create, delete).
/// </summary>
public interface IMocksManagementService
{
    /// <summary>
    /// Lists the contents of a directory at the specified path.
    /// </summary>
    /// <param name="path">The relative path within the mocks directory. Use empty string or "/" for root.</param>
    /// <returns>Directory listing containing folders and files.</returns>
    Task<DirectoryListingResponse> ListDirectoryAsync(string path);
    
    /// <summary>
    /// Creates a new mock file at the specified path with the given content.
    /// </summary>
    /// <param name="path">The full path including filename (e.g., "weather/prod/success.json").</param>
    /// <param name="content">The content to write to the file.</param>
    /// <returns>Result containing file info and whether it was committed to Git.</returns>
    Task<CreateMockResponse> CreateFileAsync(string path, string content);
    
    /// <summary>
    /// Deletes a mock file at the specified path.
    /// </summary>
    /// <param name="path">The full path including filename (e.g., "weather/prod/success.json").</param>
    /// <returns>Result containing deleted file/folder info and whether it was committed to Git.</returns>
    Task<DeleteMockResponse> DeleteFileAsync(string path);
}
