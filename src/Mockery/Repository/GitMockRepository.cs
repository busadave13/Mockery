using LibGit2Sharp;
using Mockery.Configuration;
using Mockery.Models;
using Microsoft.Extensions.Options;

namespace Mockery.Repository;

public class GitMockRepository : FileSystemMockRepositoryBase
{
    public GitMockRepository(IOptions<GitRepositoryOptions> options, ILogger<GitMockRepository> logger)
        : base(options, logger)
    {
    }
    
    /// <summary>
    /// Git mode supports commit/push operations.
    /// </summary>
    public override bool IsGitMode => true;

    public override async Task InitializeAsync()
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
                // Clean up any existing non-Git files (e.g., lost+found from empty PVC)
                if (Directory.Exists(_options.ClonePath))
                {
                    var existingFiles = Directory.GetFileSystemEntries(_options.ClonePath);
                    if (existingFiles.Length > 0)
                    {
                        _logger.LogInformation("Cleaning up {Count} existing files/directories in {Path}", existingFiles.Length, _options.ClonePath);
                        foreach (var entry in existingFiles)
                        {
                            if (Directory.Exists(entry))
                            {
                                Directory.Delete(entry, recursive: true);
                            }
                            else
                            {
                                File.Delete(entry);
                            }
                        }
                    }
                }

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

    protected override async Task RefreshInternalAsync()
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
    
    public override async Task<CreateMockResponse> CreateFileAsync(string path, string content)
    {
        // First, call base implementation to create the file
        var result = await base.CreateFileAsync(path, content);
        
        // Then commit and push to Git
        try
        {
            // Build the full relative path from the response (Path + FileName)
            var relativePath = string.IsNullOrEmpty(result.Path) 
                ? result.FileName 
                : $"{result.Path}/{result.FileName}";
            
            _logger.LogInformation("Git commit: path={Path}, relativePath={RelativePath}, repoPath={RepoPath}", 
                path, relativePath, _options.ClonePath);
            
            await CommitAndPushAsync($"Add {result.FileName}", relativePath);
            
            return result with { CommittedToGit = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit and push file creation for {Path}. Exception: {Message}", path, ex.Message);
            // Return result but indicate Git commit failed
            return result;
        }
    }
    
    public override async Task<DeleteMockResponse> DeleteFileAsync(string path)
    {
        // First, call base implementation to delete the file
        var result = await base.DeleteFileAsync(path);
        
        // Then commit and push to Git
        try
        {
            await CommitAndPushAsync($"Delete {Path.GetFileName(path)}", path);
            
            return result with { CommittedToGit = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit and push file deletion for {Path}", path);
            // Return result but indicate Git commit failed
            return result;
        }
    }
    
    private async Task CommitAndPushAsync(string commitMessage, string filePath)
    {
        await Task.Run(() =>
        {
            using var repo = new LibGit2Sharp.Repository(_options.ClonePath);
            
            // Normalize the file path to be relative to the repository root
            var normalizedPath = filePath.Replace('\\', '/');
            while (normalizedPath.StartsWith('/'))
            {
                normalizedPath = normalizedPath.Substring(1);
            }
            
            _logger.LogInformation("Staging file: {NormalizedPath} in repo: {RepoPath}", normalizedPath, _options.ClonePath);
            
            // Verify the file exists before staging
            var absolutePath = Path.Combine(_options.ClonePath, normalizedPath);
            if (!File.Exists(absolutePath))
            {
                throw new FileNotFoundException($"Cannot stage file - file not found at: {absolutePath}");
            }
            
            // Stage the specific file using relative path
            Commands.Stage(repo, normalizedPath);
            
            // Log status after staging
            var status = repo.RetrieveStatus();
            var stagedCount = status.Staged.Count();
            var modifiedCount = status.Modified.Count();
            var addedCount = status.Added.Count();
            _logger.LogInformation("Git status after staging: Staged={Staged}, Modified={Modified}, Added={Added}, IsDirty={IsDirty}", 
                stagedCount, modifiedCount, addedCount, status.IsDirty);
            
            // Check if there are any changes to commit
            if (!status.IsDirty)
            {
                _logger.LogWarning("No changes to commit for {Path} - file may already be committed", filePath);
                return;
            }
            
            // Create commit
            var signature = new Signature("Mockery", "mockery@localhost", DateTimeOffset.Now);
            repo.Commit(commitMessage, signature, signature);
            _logger.LogInformation("Committed changes: {Message}", commitMessage);
            
            // Check if access token is configured for push
            if (string.IsNullOrEmpty(_options.AccessToken))
            {
                _logger.LogWarning("No access token configured - push may fail for private repositories or GitHub");
            }
            
            // Push to remote
            var remote = repo.Network.Remotes["origin"];
            var pushOptions = new PushOptions();
            
            if (!string.IsNullOrEmpty(_options.AccessToken))
            {
                pushOptions.CredentialsProvider = (url, user, cred) =>
                    new UsernamePasswordCredentials
                    {
                        Username = _options.AccessToken,
                        Password = string.Empty
                    };
            }
            
            var pushRefSpec = $"refs/heads/{_options.Branch}";
            _logger.LogInformation("Pushing to remote: {Remote}, refspec: {RefSpec}", remote.Url, pushRefSpec);
            repo.Network.Push(remote, pushRefSpec, pushOptions);
            _logger.LogInformation("Pushed changes to {Branch}", _options.Branch);
        });
    }
}
