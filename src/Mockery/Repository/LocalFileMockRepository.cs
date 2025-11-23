using Mockery.Configuration;
using Microsoft.Extensions.Options;

namespace Mockery.Repository;

public class LocalFileMockRepository : FileSystemMockRepositoryBase
{
    public LocalFileMockRepository(IOptions<GitRepositoryOptions> options, ILogger<LocalFileMockRepository> logger)
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

            _logger.LogInformation("Initializing local file system repository at {Path}", _options.ClonePath);

            // Create mocks directory if it doesn't exist
            var mocksRootPath = Path.Combine(_options.ClonePath, "mocks");
            if (!Directory.Exists(mocksRootPath))
            {
                Directory.CreateDirectory(mocksRootPath);
                _logger.LogInformation("Created mocks directory at {Path}", mocksRootPath);
            }
            else
            {
                _logger.LogInformation("Local mocks directory already exists at {Path}", mocksRootPath);
            }

            _initialized = true;
            _logger.LogInformation("Local file system repository initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize local file system repository");
            throw;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    // RefreshAsync is inherited from base class and does nothing (no refresh needed for local files)

    /// <summary>
    /// Override to include "mocks" subdirectory for local development structure.
    /// Local structure: ClonePath/mocks/{ServiceName}/{FileId}.*
    /// </summary>
    protected override string GetMocksRootPath()
    {
        return Path.Combine(_options.ClonePath, "mocks");
    }
}
