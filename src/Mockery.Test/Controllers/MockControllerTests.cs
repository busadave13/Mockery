using System.Diagnostics.Metrics;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mockery.BusinessLogic;
using Mockery.Configuration;
using Mockery.Controllers;
using Mockery.Models;
using Mockery.Services;
using Moq;
using Xunit;

namespace Mockery.Test.Controllers;

public class MockControllerTests
{
    private readonly Mock<IMockService> _mockService;
    private readonly Mock<ILogger<MockController>> _mockLogger;
    private readonly MockeryMetrics _metrics;
    private readonly MockController _controller;

    public MockControllerTests()
    {
        _mockService = new Mock<IMockService>();
        _mockLogger = new Mock<ILogger<MockController>>();

        // Create a real MockeryMetrics with a test meter factory and mocked dependencies
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

        _controller = new MockController(_mockService.Object, _mockLogger.Object, _metrics);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetMock_WithValidMockId_ReturnsOk()
    {
        // Arrange
        _controller.Request.Headers["X-Mockery-Mock"] = "FooBar/1234";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new MockFileResult
            {
                MockId = "FooBar/1234",
                Content = "{\"test\":\"data\"}",
                ContentType = "application/json",
                ShouldReturnContent = true
            });

        // Act
        var result = await _controller.GetMock();

        // Assert
        result.Should().BeOfType<ContentResult>();
        var contentResult = (ContentResult)result;
        contentResult.Content.Should().Contain("test");
        contentResult.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetMock_WithMissingMockIdHeader_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetMock();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMock_WithMultipleMockIds_ParsesCorrectly()
    {
        // Arrange
        _controller.Request.Headers["X-Mockery-Mock"] = "FooBar/1234,FooBar/5678,Products/9012";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new MockFileResult
            {
                MockId = "FooBar/1234",
                Content = "{\"test\":\"data\"}",
                ContentType = "application/json",
                ShouldReturnContent = true
            });

        // Act
        var result = await _controller.GetMock();

        // Assert
        _mockService.Verify(x => x.GetMockAsync(
            It.Is<IEnumerable<string>>(ids => ids.Count() == 3)), Times.Once);
    }

    [Fact]
    public async Task GetMock_WhenMockNotFound_ReturnsNotFound()
    {
        // Arrange
        _controller.Request.Headers["X-Mockery-Mock"] = "FooBar/9999";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync((MockFileResult?)null);

        // Act
        var result = await _controller.GetMock();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetMock_WithCustomHeaders_SetsResponseHeaders()
    {
        // Arrange
        _controller.Request.Headers["X-Mockery-Mock"] = "FooBar/1234";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new MockFileResult
            {
                MockId = "FooBar/1234",
                Content = "{\"test\":\"data\"}",
                ContentType = "application/json",
                ShouldReturnContent = true,
                CustomHeaders = new Dictionary<string, string>
                {
                    { "X-Custom-Header", "CustomValue" }
                }
            });

        // Act
        var result = await _controller.GetMock();

        // Assert
        _controller.Response.Headers["X-Custom-Header"].ToString().Should().Be("CustomValue");
    }

    [Fact]
    public async Task GetMock_WithStatusFile_ReturnsStatusCode()
    {
        // Arrange
        _controller.Request.Headers["X-Mockery-Mock"] = "FooBar/504";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new MockFileResult
            {
                MockId = "FooBar/504",
                Content = "{\"error\":\"Gateway Timeout\"}",
                ContentType = "application/json",
                ShouldReturnContent = true,
                StatusCode = 504
            });

        // Act
        var result = await _controller.GetMock();

        // Assert
        _controller.Response.StatusCode.Should().Be(504);
        result.Should().BeOfType<ContentResult>();
    }

    [Fact]
    public async Task GetMock_WithStatusFile204_ReturnsNoContent()
    {
        // Arrange
        _controller.Request.Headers["X-Mockery-Mock"] = "FooBar/204";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new MockFileResult
            {
                MockId = "FooBar/204",
                ShouldReturnContent = false,
                StatusCode = 204,
                CustomHeaders = new Dictionary<string, string>()
            });

        // Act
        var result = await _controller.GetMock();

        // Assert
        result.Should().BeOfType<EmptyResult>();
        _controller.Response.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task GetMock_WithEmptyMockId_ReturnsBadRequest()
    {
        // Arrange
        _controller.Request.Headers["X-Mockery-Mock"] = "";

        // Act
        var result = await _controller.GetMock();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMock_WithWhitespaceMockId_ReturnsBadRequest()
    {
        // Arrange
        _controller.Request.Headers["X-Mockery-Mock"] = "   ";

        // Act
        var result = await _controller.GetMock();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
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
