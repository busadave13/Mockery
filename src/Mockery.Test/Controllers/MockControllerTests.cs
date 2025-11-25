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
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>()))
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
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>()))
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
            It.Is<IEnumerable<string>>(ids => ids.Count() == 3)), Times.Once);
    }

    [Fact]
    public async Task GetMock_WhenMockNotFound_ReturnsNotFound()
    {
        // Arrange
        _controller.Request.Headers["X-Mock-ID"] = "FooBar/9999";
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
        _controller.Request.Headers["X-Mock-ID"] = "FooBar/1234";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>()))
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

    [Fact]
    public async Task GetMock_WithStatusFile_ReturnsStatusCode()
    {
        // Arrange
        _controller.Request.Headers["X-Mock-ID"] = "FooBar/504";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new MockFileResult
            {
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
        _controller.Request.Headers["X-Mock-ID"] = "FooBar/204";
        _mockService.Setup(x => x.GetMockAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new MockFileResult
            {
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
        _controller.Request.Headers["X-Mock-ID"] = "";

        // Act
        var result = await _controller.GetMock();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetMock_WithWhitespaceMockId_ReturnsBadRequest()
    {
        // Arrange
        _controller.Request.Headers["X-Mock-ID"] = "   ";

        // Act
        var result = await _controller.GetMock();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
