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
        _mockRepository.Setup(x => x.FindStatusFileAsync("FooBar", "9999"))
            .ReturnsAsync(((int, string?)?)null);

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetMockAsync_WithStatusFile_ReturnsStatusCodeFromFile()
    {
        // Arrange
        var mockIds = new[] { "FooBar/504" };
        _mockRepository.Setup(x => x.FindStatusFileAsync("FooBar", "504"))
            .ReturnsAsync((504, "{\"error\":\"Gateway Timeout\"}"));
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(504);
        result.Content.Should().Contain("Gateway Timeout");
        result.ContentType.Should().Be("application/json");
        result.ShouldReturnContent.Should().BeTrue();
    }

    [Fact]
    public async Task GetMockAsync_WithStatusFile204_ReturnsStatusCodeWithNoContent()
    {
        // Arrange
        var mockIds = new[] { "FooBar/204" };
        _mockRepository.Setup(x => x.FindStatusFileAsync("FooBar", "204"))
            .ReturnsAsync((204, (string?)null));

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(204);
        result.Content.Should().BeEmpty();
        result.ShouldReturnContent.Should().BeFalse(); // 204 No Content semantics
    }

    [Fact]
    public async Task GetMockAsync_WithStatusFileAndHeadersFile_ReturnsBoth()
    {
        // Arrange
        var mockIds = new[] { "FooBar/500" };
        var customHeaders = new Dictionary<string, string>
        {
            { "X-Error-Code", "INTERNAL_ERROR" }
        };

        _mockRepository.Setup(x => x.FindStatusFileAsync("FooBar", "500"))
            .ReturnsAsync((500, "{\"error\":\"Internal Server Error\"}"));
        _mockRepository.Setup(x => x.FindHeadersFileAsync("FooBar", "500"))
            .ReturnsAsync(customHeaders);
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(500);
        result.Content.Should().Contain("Internal Server Error");
        result.CustomHeaders.Should().ContainKey("X-Error-Code");
        result.CustomHeaders["X-Error-Code"].Should().Be("INTERNAL_ERROR");
    }

    [Fact]
    public async Task GetMockAsync_StatusFileTakesPriorityOverRegularMockFile()
    {
        // Arrange
        var mockIds = new[] { "FooBar/500" };
        _mockRepository.Setup(x => x.FindStatusFileAsync("FooBar", "500"))
            .ReturnsAsync((500, "{\"status\":\"file\"}"));
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(500);
        result.Content.Should().Contain("status");
        
        // Verify FindMockFileAsync was NOT called since status file was found
        _mockRepository.Verify(x => x.FindMockFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetMockAsync_WhenNoStatusFile_FallsBackToRegularMockFile()
    {
        // Arrange
        var mockIds = new[] { "FooBar/test" };
        _mockRepository.Setup(x => x.FindStatusFileAsync("FooBar", "test"))
            .ReturnsAsync(((int, string?)?)null);
        _mockRepository.Setup(x => x.FindMockFileAsync("FooBar", "test"))
            .ReturnsAsync(("{\"regular\":\"mock\"}", ".json"));
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().BeNull(); // No status code from file
        result.Content.Should().Contain("regular");
    }

    [Fact]
    public async Task GetMockAsync_WithStatusFile400_ReturnsContentWithStatusCode()
    {
        // Arrange
        var mockIds = new[] { "FooBar/400" };
        _mockRepository.Setup(x => x.FindStatusFileAsync("FooBar", "400"))
            .ReturnsAsync((400, "{\"error\":\"Bad Request\"}"));
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(400);
        result.ShouldReturnContent.Should().BeTrue(); // 400 returns content
        result.Content.Should().Contain("Bad Request");
    }

    [Fact]
    public async Task GetMockAsync_WithStatusFile401_ReturnsContentWithStatusCode()
    {
        // Arrange
        var mockIds = new[] { "FooBar/401" };
        _mockRepository.Setup(x => x.FindStatusFileAsync("FooBar", "401"))
            .ReturnsAsync((401, "{\"error\":\"Unauthorized\"}"));
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(401);
        result.ShouldReturnContent.Should().BeTrue();
        result.Content.Should().Contain("Unauthorized");
    }

    [Fact]
    public async Task GetMockAsync_WithEmptyStatusFile500_ReturnsStatusCodeWithEmptyContent()
    {
        // Arrange
        var mockIds = new[] { "FooBar/500" };
        _mockRepository.Setup(x => x.FindStatusFileAsync("FooBar", "500"))
            .ReturnsAsync((500, ""));
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(500);
        result.ShouldReturnContent.Should().BeTrue();
        result.Content.Should().BeEmpty(); // Empty content from file
    }

    [Fact]
    public async Task GetMockAsync_WithSubfolderPath_ReturnsMockFile()
    {
        // Arrange
        var mockIds = new[] { "FooBar/Staging/1234" };
        _mockRepository.Setup(x => x.FindMockFileAsync("FooBar/Staging", "1234"))
            .ReturnsAsync(("{\"env\":\"staging\"}", ".json"));
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("staging");
        result.ContentType.Should().Be("application/json");
        _mockRepository.Verify(x => x.FindMockFileAsync("FooBar/Staging", "1234"), Times.Once);
    }

    [Fact]
    public async Task GetMockAsync_WithDeepSubfolderPath_ReturnsMockFile()
    {
        // Arrange
        var mockIds = new[] { "FooBar/Staging/Private/test" };
        _mockRepository.Setup(x => x.FindMockFileAsync("FooBar/Staging/Private", "test"))
            .ReturnsAsync(("{\"deep\":\"nested\"}", ".json"));
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Contain("nested");
        _mockRepository.Verify(x => x.FindMockFileAsync("FooBar/Staging/Private", "test"), Times.Once);
    }

    [Fact]
    public async Task GetMockAsync_WithSubfolderPathAndHeaders_ReturnsCustomHeaders()
    {
        // Arrange
        var mockIds = new[] { "Service/Env/fileId" };
        var customHeaders = new Dictionary<string, string>
        {
            { "X-Environment", "test" }
        };

        _mockRepository.Setup(x => x.FindMockFileAsync("Service/Env", "fileId"))
            .ReturnsAsync(("{}", ".json"));
        _mockRepository.Setup(x => x.FindHeadersFileAsync("Service/Env", "fileId"))
            .ReturnsAsync(customHeaders);
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.CustomHeaders.Should().ContainKey("X-Environment");
        _mockRepository.Verify(x => x.FindHeadersFileAsync("Service/Env", "fileId"), Times.Once);
    }

    [Fact]
    public async Task GetMockAsync_WithSubfolderPathAndStatusFile_ReturnsStatusCode()
    {
        // Arrange
        var mockIds = new[] { "Service/Production/500" };
        _mockRepository.Setup(x => x.FindStatusFileAsync("Service/Production", "500"))
            .ReturnsAsync((500, "{\"error\":\"Production error\"}"));
        _mockContentTypeResolver.Setup(x => x.GetContentType(".json"))
            .Returns("application/json");

        // Act
        var result = await _service.GetMockAsync(mockIds);

        // Assert
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(500);
        result.Content.Should().Contain("Production error");
        _mockRepository.Verify(x => x.FindStatusFileAsync("Service/Production", "500"), Times.Once);
    }
}
