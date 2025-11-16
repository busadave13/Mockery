using LibGit2Sharp;
using Mockery.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Mockery.Repository;

public class GitMockRepository : IGitMockRepository
{
    private readonly GitRepositoryOptions _options;
    private readonly ILogger<GitMockRepository> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private bool _initialized = false;

    public GitMockRepository(IOptions<GitRepositoryOptions> options, ILogger<GitMockRepository> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            _logger.LogInformation("Initializing Git repository from {Url}", _options.RepositoryUrl);

            // Create clone path directory if it doesn't exist
            if (!Directory.Exists(_options.ClonePath))
            {
                Directory.CreateDirectory(_options.ClonePath);
            }

            // Check if repository is already cloned
            if (Directory.Exists(Path.Combine(_options.ClonePath, ".git")))
            {
                _logger.LogInformation("Repository already cloned at {Path}", _options.ClonePath);
                await RefreshInternalAsync();
            }
            else
            {
                _logger.LogInformation("Cloning repository to {Path}", _options.ClonePath);

                var cloneOptions = new CloneOptions();

                if (!string.IsNullOrEmpty(_options.AccessToken))
                {
                    cloneOptions.FetchOptions.CredentialsProvider = (url, user, cred) =>
                        new UsernamePasswordCredentials
                        {
                            Username = _options.AccessToken,
                            Password = string.Empty
                        };
                }

                await Task.Run(() => LibGit2Sharp.Repository.Clone(_options.RepositoryUrl, _options.ClonePath, cloneOptions));
                _logger.LogInformation("Repository cloned successfully");
            }

            _initialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Git repository");
            throw;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<(string Content, string Extension)?> FindMockFileAsync(string serviceName, string fileId)
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");
        }

        try
        {
            var mocksPath = Path.Combine(_options.ClonePath, "mocks", serviceName);

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
            var headersPath = Path.Combine(_options.ClonePath, "mocks", serviceName, $"{fileId}.headers.json");

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

    public async Task RefreshAsync()
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

    private async Task RefreshInternalAsync()
    {
        try
        {
            _logger.LogInformation("Refreshing Git repository");

            await Task.Run(() =>
            {
                using var repo = new LibGit2Sharp.Repository(_options.ClonePath);

                var options = new PullOptions
                {
                    FetchOptions = new FetchOptions()
                };

                if (!string.IsNullOrEmpty(_options.AccessToken))
                {
                    options.FetchOptions.CredentialsProvider = (url, user, cred) =>
                        new UsernamePasswordCredentials
                        {
                            Username = _options.AccessToken,
                            Password = string.Empty
                        };
                }

                var signature = new Signature("Mockery", "mockery@localhost", DateTimeOffset.Now);
                Commands.Pull(repo, signature, options);
            });

            _logger.LogInformation("Repository refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh Git repository");
            // Don't throw - we can continue serving from existing files
        }
    }
}
