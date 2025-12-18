using Mockery.Configuration;
using Mockery.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Mockery.Repository;

public abstract class FileSystemMockRepositoryBase : IGitMockRepository
{
    protected readonly GitRepositoryOptions _options;
    protected readonly ILogger _logger;
    protected readonly SemaphoreSlim _refreshLock = new(1, 1);
    protected bool _initialized = false;

    protected FileSystemMockRepositoryBase(IOptions<GitRepositoryOptions> options, ILogger logger)
    {
        _options = options.Value;
        _logger = logger;
    }
    
    /// <summary>
    /// Indicates whether this repository supports Git operations (commit/push).
    /// Override in derived classes to return true for Git mode.
    /// </summary>
    public virtual bool IsGitMode => false;

    public abstract Task InitializeAsync();

    public virtual async Task RefreshAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            await RefreshInternalAsync();
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    protected virtual Task RefreshInternalAsync()
    {
        // Default implementation: no-op
        // Derived classes can override if they need refresh logic
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the root path for mock files. Override in derived classes to customize path structure.
    /// </summary>
    protected virtual string GetMocksRootPath()
    {
        // Default: root is ClonePath (for Git mode where services are at root)
        return _options.ClonePath;
    }

    public async Task<(string Content, string Extension)?> FindMockFileAsync(string serviceName, string fileId)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");
        }

        try
        {
            var mocksPath = Path.Combine(GetMocksRootPath(), serviceName);

            if (!Directory.Exists(mocksPath))
            {
                _logger.LogWarning("Service folder not found: {ServiceName}", serviceName);
                return null;
            }

            // Search for files matching the pattern {fileId}.*
            // Exclude .headers.json and .status.json as they are special file types
            var files = Directory.GetFiles(mocksPath, $"{fileId}.*")
                .Where(f => !f.EndsWith(".headers.json", StringComparison.OrdinalIgnoreCase))
                .Where(f => !f.EndsWith(".status.json", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (files.Length == 0)
            {
                _logger.LogWarning("Mock file not found: {ServiceName}/{FileId}", serviceName, fileId);
                return null;
            }

            if (files.Length > 1)
            {
                _logger.LogWarning("Multiple mock files found for {ServiceName}/{FileId}, using first match", serviceName, fileId);
            }

            var filePath = files[0];
            var content = await File.ReadAllTextAsync(filePath);
            var extension = Path.GetExtension(filePath);

            _logger.LogInformation("Found mock file: {FilePath}", filePath);
            return (content, extension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding mock file {ServiceName}/{FileId}", serviceName, fileId);
            throw;
        }
    }

    public async Task<Dictionary<string, string>?> FindHeadersFileAsync(string serviceName, string fileId)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");
        }

        try
        {
            var headersPath = Path.Combine(GetMocksRootPath(), serviceName, $"{fileId}.headers.json");

            if (!File.Exists(headersPath))
            {
                _logger.LogDebug("Headers file not found: {Path}", headersPath);
                return null;
            }

            var content = await File.ReadAllTextAsync(headersPath);
            var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(content);

            _logger.LogInformation("Found headers file: {Path}", headersPath);
            return headers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading headers file {ServiceName}/{FileId}.headers.json", serviceName, fileId);
            return null;
        }
    }

    public async Task<(int StatusCode, string? Content)?> FindStatusFileAsync(string serviceName, string fileId)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");
        }

        try
        {
            var statusPath = Path.Combine(GetMocksRootPath(), serviceName, $"{fileId}.status.json");

            if (!File.Exists(statusPath))
            {
                _logger.LogDebug("Status file not found: {Path}", statusPath);
                return null;
            }

            // Parse the status code from the fileId (e.g., "504" from "504.status.json")
            if (!int.TryParse(fileId, out var statusCode) || statusCode < 100 || statusCode > 599)
            {
                _logger.LogWarning("Invalid status code in filename: {FileId}. Must be a valid HTTP status code (100-599)", fileId);
                return null;
            }

            // Read content from the file (can be empty)
            var content = await File.ReadAllTextAsync(statusPath);
            var trimmedContent = string.IsNullOrWhiteSpace(content) ? null : content;

            _logger.LogInformation("Found status file: {Path}, StatusCode: {StatusCode}", statusPath, statusCode);
            return (statusCode, trimmedContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading status file {ServiceName}/{FileId}.status.json", serviceName, fileId);
            return null;
        }
    }
    
    // Management API implementations
    
    public virtual Task<DirectoryListingResponse> ListDirectoryAsync(string path)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");
        }

        try
        {
            // Normalize path: empty, "/", or "//" means root
            var normalizedPath = NormalizePath(path);
            var fullPath = string.IsNullOrEmpty(normalizedPath) 
                ? GetMocksRootPath() 
                : Path.Combine(GetMocksRootPath(), normalizedPath);

            if (!Directory.Exists(fullPath))
            {
                _logger.LogWarning("Directory not found: {Path}", fullPath);
                return Task.FromResult(new DirectoryListingResponse
                {
                    Path = string.IsNullOrEmpty(normalizedPath) ? "/" : normalizedPath,
                    Items = new List<DirectoryItem>()
                });
            }

            var items = new List<DirectoryItem>();

            // Get directories
            foreach (var dir in Directory.GetDirectories(fullPath))
            {
                var dirInfo = new DirectoryInfo(dir);
                items.Add(new DirectoryItem
                {
                    Name = dirInfo.Name,
                    Type = "folder"
                });
            }

            // Get files
            foreach (var file in Directory.GetFiles(fullPath))
            {
                var fileInfo = new FileInfo(file);
                items.Add(new DirectoryItem
                {
                    Name = fileInfo.Name,
                    Type = "file",
                    Extension = fileInfo.Extension,
                    Size = fileInfo.Length
                });
            }

            _logger.LogInformation("Listed directory {Path}: {Count} items", fullPath, items.Count);

            return Task.FromResult(new DirectoryListingResponse
            {
                Path = string.IsNullOrEmpty(normalizedPath) ? "/" : normalizedPath,
                Items = items
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing directory {Path}", path);
            throw;
        }
    }
    
    public virtual async Task<CreateMockResponse> CreateFileAsync(string path, string content)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        ArgumentNullException.ThrowIfNull(content, nameof(content));

        try
        {
            var normalizedPath = NormalizePath(path);
            if (string.IsNullOrEmpty(normalizedPath))
            {
                throw new ArgumentException("Path must include a filename", nameof(path));
            }

            var fullPath = Path.Combine(GetMocksRootPath(), normalizedPath);
            var directory = Path.GetDirectoryName(fullPath);
            var fileName = Path.GetFileName(fullPath);

            if (string.IsNullOrEmpty(fileName))
            {
                throw new ArgumentException("Path must include a filename", nameof(path));
            }

            // Create directory if it doesn't exist
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation("Created directory: {Directory}", directory);
            }

            // Check if file already exists (idempotency check)
            if (File.Exists(fullPath))
            {
                throw new InvalidOperationException($"File already exists: {normalizedPath}");
            }

            // Write file content
            await File.WriteAllTextAsync(fullPath, content);
            var fileInfo = new FileInfo(fullPath);

            _logger.LogInformation("Created file: {Path}, Size: {Size} bytes", fullPath, fileInfo.Length);

            // Get the relative directory path (without filename)
            var relativePath = Path.GetDirectoryName(normalizedPath)?.Replace(Path.DirectorySeparatorChar, '/') ?? string.Empty;

            return new CreateMockResponse
            {
                Path = relativePath,
                FileName = fileName,
                Size = fileInfo.Length,
                CommittedToGit = false  // Base class doesn't support Git
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating file {Path}", path);
            throw;
        }
    }
    
    public virtual Task<DeleteMockResponse> DeleteFileAsync(string path)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        try
        {
            var normalizedPath = NormalizePath(path);
            if (string.IsNullOrEmpty(normalizedPath))
            {
                throw new ArgumentException("Path must include a filename", nameof(path));
            }

            var fullPath = Path.Combine(GetMocksRootPath(), normalizedPath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {normalizedPath}", normalizedPath);
            }

            // Delete the file
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file: {Path}", fullPath);

            // Track deleted folders
            var deletedFolders = new List<string>();
            var mocksRoot = GetMocksRootPath();

            // Delete empty parent folders up to (but not including) mocks root
            var parentDir = Path.GetDirectoryName(fullPath);
            while (!string.IsNullOrEmpty(parentDir) && 
                   !parentDir.Equals(mocksRoot, StringComparison.OrdinalIgnoreCase) &&
                   Directory.Exists(parentDir))
            {
                var entries = Directory.GetFileSystemEntries(parentDir);
                if (entries.Length == 0)
                {
                    var relativeFolderPath = Path.GetRelativePath(mocksRoot, parentDir).Replace(Path.DirectorySeparatorChar, '/');
                    Directory.Delete(parentDir);
                    deletedFolders.Add(relativeFolderPath);
                    _logger.LogInformation("Deleted empty folder: {Path}", parentDir);
                    parentDir = Path.GetDirectoryName(parentDir);
                }
                else
                {
                    break;
                }
            }

            return Task.FromResult(new DeleteMockResponse
            {
                DeletedFile = normalizedPath,
                DeletedFolders = deletedFolders,
                CommittedToGit = false  // Base class doesn't support Git
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {Path}", path);
            throw;
        }
    }
    
    /// <summary>
    /// Normalizes a path by removing leading slashes and converting to forward slashes.
    /// </summary>
    protected static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        // Trim whitespace and normalize slashes
        var normalized = path.Trim().Replace('\\', '/');
        
        // Remove leading slashes (including //)
        while (normalized.StartsWith('/'))
        {
            normalized = normalized.Substring(1);
        }
        
        // Remove trailing slashes
        while (normalized.EndsWith('/'))
        {
            normalized = normalized.Substring(0, normalized.Length - 1);
        }

        return normalized;
    }
}
