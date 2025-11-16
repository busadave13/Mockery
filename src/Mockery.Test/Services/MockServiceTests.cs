using FluentAssertions;
using Microsoft.Extensions.Logging;
using Mockery.BusinessLogic;
using Mockery.Repository;
using Mockery.Services;
using Moq;
using Xunit;

namespace Mockery.Test.Services;

public class MockServiceTests
{
    private readonly Mock<IGitMockRepository> _mockRepository;
    private readonly Mock<IContentTypeResolver> _mockContentTypeResolver;
    private readonly Mock<ILogger<MockService>> _mockLogger;
    private readonly MockService _service;

    public MockServiceTests()
    {
        _mockRepository = new Mock<IGitMockRepository>();
        _mockContentTypeResolver = new Mock<IContentTypeResolver>();
        _mockLogger = new Mock<ILogger<MockService>>();
        _service = new MockService(_mockRepository.Object, _mockContentTypeResolver.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetMockAsync_WithSingleMockId_ReturnsMockFile()
    {
        // Arrange
        var mockIds = new[] { "FooBar/1234" };
        _mockRepository.Setup(x => x.FindMockFileAsync("FooBar", "1234"))
            .ReturnsAsync(("{\"test\":\"data\"}", ".json"));
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("test");
        result.ContentType.Should().Be("application/json");
        result.ShouldReturnContent.Should().BeTrue();
    }

    [Fact]
    public async Task GetMockAsync_WithMultipleMockIds_ReturnsRandomMock()
    {
        // Arrange
        var mockIds = new[] { "FooBar/1234", "FooBar/5678" };
        _mockRepository.Setup(x => x.FindMockFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(("{\"test\":\"data\"}", ".json"));
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(x => x.FindMockFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task GetMockAsync_WithStatusCode204_DoesNotReturnContent()
    {
        // Arrange
        var mockIds = new[] { "FooBar/1234" };

        // Act
        var result = await _service.GetMockAsync(mockIds, statusCode: 204);

        // Assert
        result.Should().NotBeNull();
        result!.ShouldReturnContent.Should().BeFalse();
        _mockRepository.Verify(x => x.FindMockFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetMockAsync_WithStatusCode404_DoesNotReturnContent()
    {
        // Arrange
        var mockIds = new[] { "FooBar/1234" };

        // Act
        var result = await _service.GetMockAsync(mockIds, statusCode: 404);

        // Assert
        result.Should().NotBeNull();
        result!.ShouldReturnContent.Should().BeFalse();
    }

    [Fact]
    public async Task GetMockAsync_WithStatusCode500_ReturnsContent()
    {
        // Arrange
        var mockIds = new[] { "FooBar/1234" };
        _mockRepository.Setup(x => x.FindMockFileAsync("FooBar", "1234"))
            .ReturnsAsync(("{\"error\":\"internal error\"}", ".json"));
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds, statusCode: 500);

        // Assert
        result.Should().NotBeNull();
        result!.ShouldReturnContent.Should().BeTrue();
        result.Content.Should().Contain("error");
    }

    [Fact]
    public async Task GetMockAsync_WithHeadersFile_ReturnsCustomHeaders()
    {
        // Arrange
        var mockIds = new[] { "FooBar/1234" };
        var customHeaders = new Dictionary<string, string>
        {
            { "X-Custom-Header", "CustomValue" },
            { "Cache-Control", "no-cache" }
        };

        _mockRepository.Setup(x => x.FindMockFileAsync("FooBar", "1234"))
            .ReturnsAsync(("{\"test\":\"data\"}", ".json"));
        _mockRepository.Setup(x => x.FindHeadersFileAsync("FooBar", "1234"))
            .ReturnsAsync(customHeaders);
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.CustomHeaders.Should().HaveCount(2);
        result.CustomHeaders["X-Custom-Header"].Should().Be("CustomValue");
        result.CustomHeaders["Cache-Control"].Should().Be("no-cache");
    }

    [Fact]
    public async Task GetMockAsync_WithInvalidMockIdFormat_ReturnsNull()
    {
        // Arrange
        var mockIds = new[] { "InvalidFormat" };

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMockAsync_WithEmptyMockIds_ReturnsNull()
    {
        // Arrange
        var mockIds = Array.Empty<string>();

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMockAsync_WhenMockFileNotFound_ReturnsNull()
    {
        // Arrange
        var mockIds = new[] { "FooBar/9999" };
        _mockRepository.Setup(x => x.FindMockFileAsync("FooBar", "9999"))
            .ReturnsAsync(((string, string)?)null);

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().BeNull();
    }
}
