using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mockery.Configuration;
using Mockery.Repository;
using Mockery.Services;
using Moq;
using Xunit;

namespace Mockery.Test.Services;

public class GitRepositoryRefreshServiceTests
{
    private readonly Mock<IGitMockRepository> _mockRepository;
    private readonly Mock<ILogger<GitRepositoryRefreshService>> _mockLogger;

    public GitRepositoryRefreshServiceTests()
    {
        _mockRepository = new Mock<IGitMockRepository>();
        _mockLogger = new Mock<ILogger<GitRepositoryRefreshService>>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_DoesNotRefresh()
    {
        // Arrange
        var settings = new MockRepositorySettings
        {
            Type = "Git",
            Git = new GitSettings
            {
                AutoRefresh = new AutoRefreshSettings
                {
                    Enabled = false,
                    IntervalMinutes = 1
                }
            }
        };

        var options = Options.Create(settings);
        var service = new GitRepositoryRefreshService(_mockRepository.Object, options, _mockLogger.Object);
        var cts = new CancellationTokenSource();

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(100); // Give it time to start
        cts.Cancel();
        await executeTask;

        // Assert
        _mockRepository.Verify(x => x.RefreshAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEnabled_RefreshesRepository()
    {
        // Arrange
        var settings = new MockRepositorySettings
        {
            Type = "Git",
            Git = new GitSettings
            {
                AutoRefresh = new AutoRefreshSettings
                {
                    Enabled = true,
                    IntervalSeconds = 1 // 1 second for fast testing
                }
            }
        };

        var options = Options.Create(settings);
        var service = new GitRepositoryRefreshService(_mockRepository.Object, options, _mockLogger.Object);
        var cts = new CancellationTokenSource();

        _mockRepository.Setup(x => x.RefreshAsync()).Returns(Task.CompletedTask);

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(1.5)); // Wait for first refresh
        cts.Cancel();
        await executeTask;

        // Assert
        _mockRepository.Verify(x => x.RefreshAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshThrowsException_ContinuesRunning()
    {
        // Arrange
        var settings = new MockRepositorySettings
        {
            Type = "Git",
            Git = new GitSettings
            {
                AutoRefresh = new AutoRefreshSettings
                {
                    Enabled = true,
                    IntervalSeconds = 1 // 1 second for fast testing
                }
            }
        };

        var options = Options.Create(settings);
        var service = new GitRepositoryRefreshService(_mockRepository.Object, options, _mockLogger.Object);
        var cts = new CancellationTokenSource();

        var callCount = 0;
        _mockRepository.Setup(x => x.RefreshAsync())
            .Returns(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new Exception("Simulated error");
                }
                return Task.CompletedTask;
            });

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(2.5)); // Wait for two refresh cycles
        cts.Cancel();
        await executeTask;

        // Assert
        _mockRepository.Verify(x => x.RefreshAsync(), Times.AtLeast(2));
    }

    [Fact]
    public async Task ExecuteAsync_StopsGracefullyOnCancellation()
    {
        // Arrange
        var settings = new MockRepositorySettings
        {
            Type = "Git",
            Git = new GitSettings
            {
                AutoRefresh = new AutoRefreshSettings
                {
                    Enabled = true,
                    IntervalMinutes = 10
                }
            }
        };

        var options = Options.Create(settings);
        var service = new GitRepositoryRefreshService(_mockRepository.Object, options, _mockLogger.Object);
        var cts = new CancellationTokenSource();

        _mockRepository.Setup(x => x.RefreshAsync()).Returns(Task.CompletedTask);

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(100); // Give it time to start
        cts.Cancel();

        // Should complete without throwing
        var stopTask = service.StopAsync(CancellationToken.None);
        await Task.WhenAll(executeTask, stopTask);

        // Assert - should not throw
        stopTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_RespectsConfiguredInterval()
    {
        // Arrange
        var settings = new MockRepositorySettings
        {
            Type = "Git",
            Git = new GitSettings
            {
                AutoRefresh = new AutoRefreshSettings
                {
                    Enabled = true,
                    IntervalSeconds = 1 // 1 second for fast testing
                }
            }
        };

        var options = Options.Create(settings);
        var service = new GitRepositoryRefreshService(_mockRepository.Object, options, _mockLogger.Object);
        var cts = new CancellationTokenSource();

        var refreshTimes = new List<DateTime>();
        _mockRepository.Setup(x => x.RefreshAsync())
            .Returns(() =>
            {
                refreshTimes.Add(DateTime.UtcNow);
                return Task.CompletedTask;
            });

        // Act
        var executeTask = service.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(2.5)); // Wait for two refresh cycles
        cts.Cancel();
        await executeTask;

        // Assert
        refreshTimes.Should().HaveCountGreaterOrEqualTo(2);
        if (refreshTimes.Count >= 2)
        {
            var interval = refreshTimes[1] - refreshTimes[0];
            interval.Should().BeCloseTo(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(200));
        }
    }
}
