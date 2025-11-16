using LibGit2Sharp;
using Mockery.Configuration;
using Microsoft.Extensions.Options;

namespace Mockery.Repository;

public class GitMockRepository : FileSystemMockRepositoryBase
{
    public GitMockRepository(IOptions<GitRepositoryOptions> options, ILogger<GitMockRepository> logger)
        : base(options, logger)
    {
    }

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
}
