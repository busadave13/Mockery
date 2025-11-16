using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mockery.BusinessLogic;
using Mockery.Controllers;
using Mockery.Models;
using Moq;
using Xunit;

namespace Mockery.Test.Controllers;

public class MockControllerTests
{
    private readonly Mock<IMockService> _mockService;
    private readonly Mock<ILogger<MockController>> _mockLogger;
    private readonly MockController _controller;

    public MockControllerTests()
    {
        _mockService = new Mock<IMockService>();
        _mockLogger = new Mock<ILogger<MockController>>();
        _controller = new MockController(_mockService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetMock_WithValidMockId_ReturnsOk()
    {
        // Arrange
        _controller.Request.Headers["X-Mock-ID"] = "FooBar/1234";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>(), null))
            .ReturnsAsync(new MockFileResult
            {
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
        _controller.Request.Headers["X-Mock-ID"] = "FooBar/1234,FooBar/5678,Products/9012";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>(), null))
            .ReturnsAsync(new MockFileResult
            {
                Content = "{\"test\":\"data\"}",
                ContentType = "application/json",
                ShouldReturnContent = true
            });

        // Act
        var result = await _controller.GetMock();

        // Assert
        _mockService.Verify(x => x.GetMockAsync(
            It.Is<IEnumerable<string>>(ids => ids.Count() == 3),
            null), Times.Once);
    }

    [Fact]
    public async Task GetMock_WithValidStatusCode_PassesToService()
    {
        // Arrange
        _controller.Request.Headers["X-Mock-ID"] = "FooBar/1234";
        _controller.Request.Headers["X-Mock-StatusCode"] = "500";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>(), 500))
            .ReturnsAsync(new MockFileResult
            {
                Content = "{\"error\":\"internal error\"}",
                ContentType = "application/json",
                ShouldReturnContent = true
            });

        // Act
        var result = await _controller.GetMock();

        // Assert
        _mockService.Verify(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>(), 500), Times.Once);
        _controller.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetMock_WithInvalidStatusCode_ReturnsBadRequest()
    {
        // Arrange
        _controller.Request.Headers["X-Mock-ID"] = "FooBar/1234";
        _controller.Request.Headers["X-Mock-StatusCode"] = "999";

        // Act
        var result = await _controller.GetMock();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMock_WithStatusCode204_ReturnsEmptyResult()
    {
        // Arrange
        _controller.Request.Headers["X-Mock-ID"] = "FooBar/1234";
        _controller.Request.Headers["X-Mock-StatusCode"] = "204";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>(), 204))
            .ReturnsAsync(new MockFileResult
            {
                ShouldReturnContent = false,
                CustomHeaders = new Dictionary<string, string>()
            });

        // Act
        var result = await _controller.GetMock();

        // Assert
        result.Should().BeOfType<EmptyResult>();
        _controller.Response.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task GetMock_WhenMockNotFound_ReturnsNotFound()
    {
        // Arrange
        _controller.Request.Headers["X-Mock-ID"] = "FooBar/9999";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>(), null))
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
        _controller.Request.Headers["X-Mock-ID"] = "FooBar/1234";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>(), null))
            .ReturnsAsync(new MockFileResult
            {
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
}
