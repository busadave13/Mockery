using Mockery.Configuration;
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
            var files = Directory.GetFiles(mocksPath, $"{fileId}.*")
                .Where(f => !f.EndsWith(".headers.json", StringComparison.OrdinalIgnoreCase))
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
}
