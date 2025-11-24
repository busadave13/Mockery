using Microsoft.Extensions.Options;
using Mockery.Configuration;
using Mockery.Repository;

namespace Mockery.Services;

public class GitRepositoryRefreshService : BackgroundService
{
    private readonly IGitMockRepository _repository;
    private readonly MockRepositorySettings _settings;
    private readonly ILogger<GitRepositoryRefreshService> _logger;

    public GitRepositoryRefreshService(
        IGitMockRepository repository,
        IOptions<MockRepositorySettings> settings,
        ILogger<GitRepositoryRefreshService> logger)
    {
        _repository = repository;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Check if auto-refresh is enabled
        if (!_settings.Git.AutoRefresh.Enabled)
        {
            _logger.LogInformation("Git repository auto-refresh is disabled");
            return;
        }

        var intervalMinutes = _settings.Git.AutoRefresh.IntervalMinutes;
        var interval = TimeSpan.FromMinutes(intervalMinutes);

        _logger.LogInformation(
            "Git repository auto-refresh enabled with interval of {IntervalMinutes} minutes",
            intervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for the configured interval
                await Task.Delay(interval, stoppingToken);

                // Refresh the repository
                _logger.LogInformation("Starting Git repository refresh");
                await _repository.RefreshAsync();
                _logger.LogInformation("Git repository refresh completed successfully");
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                _logger.LogInformation("Git repository refresh service is stopping");
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue running
                _logger.LogError(
                    ex,
                    "Error occurred during Git repository refresh. Will retry after {IntervalMinutes} minutes",
                    intervalMinutes);
            }
        }
    }
}
