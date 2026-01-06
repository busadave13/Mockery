using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mockery.Configuration;
using Mockery.Middleware;
using Mockery.Services;
using Moq;
using Xunit;

namespace Mockery.Test.Middleware;

public class ThrottlingMiddlewareTests
{
    private readonly Mock<IThrottlingService> _throttlingServiceMock;
    private readonly Mock<IOptionsMonitor<ThrottlingOptions>> _optionsMonitorMock;
    private readonly Mock<ILogger<ThrottlingMiddleware>> _loggerMock;
    private readonly MockeryMetrics _metrics;
    private readonly Mock<RequestDelegate> _nextMock;
    private bool _nextWasCalled;

    public ThrottlingMiddlewareTests()
    {
        _throttlingServiceMock = new Mock<IThrottlingService>();
        _optionsMonitorMock = new Mock<IOptionsMonitor<ThrottlingOptions>>();
        _loggerMock = new Mock<ILogger<ThrottlingMiddleware>>();
        
        // Create a real MockeryMetrics with test dependencies
        var meterFactory = new TestMeterFactory();
        var throttlingOptions = new Mock<IOptionsMonitor<ThrottlingOptions>>();
        throttlingOptions.Setup(x => x.CurrentValue).Returns(new ThrottlingOptions
        {
            Enabled = true,
            RequestsPerSecond = 100,
            BurstSize = 50
        });
        var throttlingService = new Mock<IThrottlingService>();
        throttlingService.Setup(x => x.AvailableTokens).Returns(50);
        _metrics = new MockeryMetrics(meterFactory, throttlingOptions.Object, throttlingService.Object);
        
        _nextMock = new Mock<RequestDelegate>();
        _nextWasCalled = false;
    }

    private ThrottlingMiddleware CreateMiddleware(ThrottlingOptions options, RequestDelegate? next = null)
    {
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);
        
        var actualNext = next ?? ((ctx) =>
        {
            _nextWasCalled = true;
            return Task.CompletedTask;
        });
        
        return new ThrottlingMiddleware(
            actualNext,
            _throttlingServiceMock.Object,
            _optionsMonitorMock.Object,
            _loggerMock.Object,
            _metrics);
    }

    [Fact]
    public async Task InvokeAsync_WhenDisabled_CallsNextWithoutThrottling()
    {
        // Arrange
        var options = new ThrottlingOptions { Enabled = false };
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextWasCalled.Should().BeTrue();
        _throttlingServiceMock.Verify(x => x.TryConsume(), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WhenAllowed_CallsNextAndSetsHeaders()
    {
        // Arrange
        var options = new ThrottlingOptions
        {
            Enabled = true,
            RequestsPerSecond = 100,
            BurstSize = 50,
            ExcludedPaths = Array.Empty<string>()
        };
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/mock/test";

        _throttlingServiceMock.Setup(x => x.TryConsume())
            .Returns(new ThrottleResult { IsAllowed = true, RemainingTokens = 49, Limit = 100, RetryAfterSeconds = 0 });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextWasCalled.Should().BeTrue();
        context.Response.Headers["X-RateLimit-Limit"].ToString().Should().Be("100");
        context.Response.Headers["X-RateLimit-Remaining"].ToString().Should().Be("49");
    }

    [Fact]
    public async Task InvokeAsync_WhenThrottled_Returns429()
    {
        // Arrange
        var options = new ThrottlingOptions
        {
            Enabled = true,
            RequestsPerSecond = 100,
            BurstSize = 50,
            ExcludedPaths = Array.Empty<string>()
        };
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/mock/test";

        _throttlingServiceMock.Setup(x => x.TryConsume())
            .Returns(new ThrottleResult { IsAllowed = false, RemainingTokens = 0, Limit = 100, RetryAfterSeconds = 0.5 });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextWasCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(429);
        context.Response.Headers["Retry-After"].ToString().Should().Be("0");
        context.Response.Headers["X-RateLimit-Remaining"].ToString().Should().Be("0");
    }

    [Fact]
    public async Task InvokeAsync_ExcludedPath_SkipsThrottling()
    {
        // Arrange
        var options = new ThrottlingOptions
        {
            Enabled = true,
            RequestsPerSecond = 100,
            BurstSize = 50,
            ExcludedPaths = new[] { "/health", "/metrics" }
        };
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/health/live";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextWasCalled.Should().BeTrue();
        _throttlingServiceMock.Verify(x => x.TryConsume(), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_MetricsPath_SkipsThrottling()
    {
        // Arrange
        var options = new ThrottlingOptions
        {
            Enabled = true,
            RequestsPerSecond = 100,
            BurstSize = 50,
            ExcludedPaths = new[] { "/health", "/metrics" }
        };
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/metrics";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _nextWasCalled.Should().BeTrue();
        _throttlingServiceMock.Verify(x => x.TryConsume(), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_NonExcludedPath_AppliesThrottling()
    {
        // Arrange
        var options = new ThrottlingOptions
        {
            Enabled = true,
            RequestsPerSecond = 100,
            BurstSize = 50,
            ExcludedPaths = new[] { "/health", "/metrics" }
        };
        var middleware = CreateMiddleware(options);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/mock/test";

        _throttlingServiceMock.Setup(x => x.TryConsume())
            .Returns(new ThrottleResult { IsAllowed = true, RemainingTokens = 49, Limit = 100, RetryAfterSeconds = 0 });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _throttlingServiceMock.Verify(x => x.TryConsume(), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullNext_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ThrottlingOptions { Enabled = true };
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);

        // Act & Assert
        var act = () => new ThrottlingMiddleware(
            null!,
            _throttlingServiceMock.Object,
            _optionsMonitorMock.Object,
            _loggerMock.Object,
            _metrics);
        
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("next");
    }

    [Fact]
    public void Constructor_WithNullThrottlingService_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ThrottlingOptions { Enabled = true };
        _optionsMonitorMock.Setup(x => x.CurrentValue).Returns(options);
        RequestDelegate next = ctx => Task.CompletedTask;

        // Act & Assert
        var act = () => new ThrottlingMiddleware(
            next,
            null!,
            _optionsMonitorMock.Object,
            _loggerMock.Object,
            _metrics);
        
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("throttlingService");
    }

    // Test helper class for creating meters in tests
    private class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = new();

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options.Name, options.Version);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
            {
                meter.Dispose();
            }
            _meters.Clear();
        }
    }
}
