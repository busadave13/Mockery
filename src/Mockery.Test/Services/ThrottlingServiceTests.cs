using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mockery.Configuration;
using Mockery.Services;
using Moq;
using Xunit;

namespace Mockery.Test.Services;

public class ThrottlingServiceTests
{
    private readonly Mock<ILogger<ThrottlingService>> _loggerMock;

    public ThrottlingServiceTests()
    {
        _loggerMock = new Mock<ILogger<ThrottlingService>>();
    }

    private ThrottlingService CreateService(ThrottlingOptions options)
    {
        var optionsMonitor = new Mock<IOptionsMonitor<ThrottlingOptions>>();
        optionsMonitor.Setup(x => x.CurrentValue).Returns(options);
        optionsMonitor.Setup(x => x.OnChange(It.IsAny<Action<ThrottlingOptions, string?>>()))
            .Returns(Mock.Of<IDisposable>());
        
        return new ThrottlingService(optionsMonitor.Object, _loggerMock.Object);
    }

    [Fact]
    public void TryConsume_WithTokensAvailable_ReturnsAllowed()
    {
        // Arrange
        var options = new ThrottlingOptions
        {
            Enabled = true,
            RequestsPerSecond = 100,
            BurstSize = 10
        };
        var service = CreateService(options);

        // Act
        var result = service.TryConsume();

        // Assert
        result.IsAllowed.Should().BeTrue();
        result.RemainingTokens.Should().Be(9); // Started with 10 (BurstSize), consumed 1
        result.Limit.Should().Be(100);
    }

    [Fact]
    public void TryConsume_ExhaustingBurst_EventuallyDenies()
    {
        // Arrange
        var options = new ThrottlingOptions
        {
            Enabled = true,
            RequestsPerSecond = 10,
            BurstSize = 5
        };
        var service = CreateService(options);

        // Act - consume all tokens
        for (int i = 0; i < 5; i++)
        {
            var result = service.TryConsume();
            result.IsAllowed.Should().BeTrue($"Request {i + 1} should be allowed");
        }

        // Next request should be denied
        var deniedResult = service.TryConsume();

        // Assert
        deniedResult.IsAllowed.Should().BeFalse();
        deniedResult.RemainingTokens.Should().Be(0);
        deniedResult.RetryAfterSeconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TryConsume_ReturnsCorrectLimit()
    {
        // Arrange
        var options = new ThrottlingOptions
        {
            Enabled = true,
            RequestsPerSecond = 250,
            BurstSize = 50
        };
        var service = CreateService(options);

        // Act
        var result = service.TryConsume();

        // Assert
        result.Limit.Should().Be(250);
    }

    [Fact]
    public void TryConsume_RemainingTokensNeverNegative()
    {
        // Arrange
        var options = new ThrottlingOptions
        {
            Enabled = true,
            RequestsPerSecond = 10,
            BurstSize = 2
        };
        var service = CreateService(options);

        // Act - exhaust all tokens and then some
        for (int i = 0; i < 10; i++)
        {
            var result = service.TryConsume();
            
            // Assert
            result.RemainingTokens.Should().BeGreaterOrEqualTo(0);
        }
    }

    [Fact]
    public void TryConsume_RetryAfterSeconds_IsPositiveWhenDenied()
    {
        // Arrange
        var options = new ThrottlingOptions
        {
            Enabled = true,
            RequestsPerSecond = 10,
            BurstSize = 1
        };
        var service = CreateService(options);

        // Act - consume the only token
        service.TryConsume();
        var result = service.TryConsume();

        // Assert
        result.IsAllowed.Should().BeFalse();
        result.RetryAfterSeconds.Should().BeGreaterThan(0);
        result.RetryAfterSeconds.Should().BeLessOrEqualTo(1.0); // At 10 req/s, max wait is ~0.1s per token
    }

    [Fact]
    public void Constructor_WithNullOptionsMonitor_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ThrottlingService(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("optionsMonitor");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsMonitor = new Mock<IOptionsMonitor<ThrottlingOptions>>();
        optionsMonitor.Setup(x => x.CurrentValue).Returns(new ThrottlingOptions());
        optionsMonitor.Setup(x => x.OnChange(It.IsAny<Action<ThrottlingOptions, string?>>()))
            .Returns(Mock.Of<IDisposable>());

        // Act & Assert
        var act = () => new ThrottlingService(optionsMonitor.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

}
