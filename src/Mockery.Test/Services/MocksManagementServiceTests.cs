using FluentAssertions;
using Microsoft.Extensions.Logging;
using Mockery.BusinessLogic;
using Mockery.Models;
using Mockery.Repository;
using Moq;
using Xunit;

namespace Mockery.Test.Services;

public class MocksManagementServiceTests
{
    private readonly Mock<IGitMockRepository> _mockRepository;
    private readonly Mock<ILogger<MocksManagementService>> _mockLogger;
    private readonly MocksManagementService _service;

    public MocksManagementServiceTests()
    {
        _mockRepository = new Mock<IGitMockRepository>();
        _mockLogger = new Mock<ILogger<MocksManagementService>>();
        _service = new MocksManagementService(_mockRepository.Object, _mockLogger.Object);
    }

    #region ListDirectoryAsync Tests

    [Fact]
    public async Task ListDirectoryAsync_WithRootPath_CallsRepository()
    {
        // Arrange
        var expectedResponse = new DirectoryListingResponse
        {
            Path = "/",
            Items = new List<DirectoryItem>
            {
                new() { Name = "FooBar", Type = "folder" }
            }
        };
        _mockRepository.Setup(r => r.ListDirectoryAsync(string.Empty))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.ListDirectoryAsync(string.Empty);

        // Assert
        result.Should().Be(expectedResponse);
        _mockRepository.Verify(r => r.ListDirectoryAsync(string.Empty), Times.Once);
    }

    [Fact]
    public async Task ListDirectoryAsync_WithPath_CallsRepositoryWithPath()
    {
        // Arrange
        var expectedResponse = new DirectoryListingResponse
        {
            Path = "weather/prod",
            Items = new List<DirectoryItem>()
        };
        _mockRepository.Setup(r => r.ListDirectoryAsync("weather/prod"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.ListDirectoryAsync("weather/prod");

        // Assert
        result.Path.Should().Be("weather/prod");
        _mockRepository.Verify(r => r.ListDirectoryAsync("weather/prod"), Times.Once);
    }

    #endregion

    #region CreateFileAsync Tests

    [Fact]
    public async Task CreateFileAsync_WithValidPath_CallsRepository()
    {
        // Arrange
        var content = "{\"temp\": 72}";
        var expectedResponse = new CreateMockResponse
        {
            Path = "weather/prod",
            FileName = "success.json",
            Size = 15,
            CommittedToGit = false
        };
        _mockRepository.Setup(r => r.CreateFileAsync("weather/prod/success.json", content))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.CreateFileAsync("weather/prod/success.json", content);

        // Assert
        result.Should().Be(expectedResponse);
        _mockRepository.Verify(r => r.CreateFileAsync("weather/prod/success.json", content), Times.Once);
    }

    [Fact]
    public async Task CreateFileAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act
        var act = () => _service.CreateFileAsync("", "content");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateFileAsync_WithNullPath_ThrowsArgumentException()
    {
        // Act
        var act = () => _service.CreateFileAsync(null!, "content");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateFileAsync_WithNullContent_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _service.CreateFileAsync("path/file.json", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CreateFileAsync_WithGitMode_ReturnsCommittedTrue()
    {
        // Arrange
        var expectedResponse = new CreateMockResponse
        {
            Path = "weather/prod",
            FileName = "success.json",
            Size = 15,
            CommittedToGit = true
        };
        _mockRepository.Setup(r => r.CreateFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.CreateFileAsync("weather/prod/success.json", "content");

        // Assert
        result.CommittedToGit.Should().BeTrue();
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_WithValidPath_CallsRepository()
    {
        // Arrange
        var expectedResponse = new DeleteMockResponse
        {
            DeletedFile = "weather/prod/success.json",
            DeletedFolders = new List<string>(),
            CommittedToGit = false
        };
        _mockRepository.Setup(r => r.DeleteFileAsync("weather/prod/success.json"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.DeleteFileAsync("weather/prod/success.json");

        // Assert
        result.Should().Be(expectedResponse);
        _mockRepository.Verify(r => r.DeleteFileAsync("weather/prod/success.json"), Times.Once);
    }

    [Fact]
    public async Task DeleteFileAsync_WithEmptyPath_ThrowsArgumentException()
    {
        // Act
        var act = () => _service.DeleteFileAsync("");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteFileAsync_WithNullPath_ThrowsArgumentException()
    {
        // Act
        var act = () => _service.DeleteFileAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteFileAsync_WithDeletedFolders_ReturnsAllDeleted()
    {
        // Arrange
        var expectedResponse = new DeleteMockResponse
        {
            DeletedFile = "weather/prod/success.json",
            DeletedFolders = new List<string> { "weather/prod", "weather" },
            CommittedToGit = true
        };
        _mockRepository.Setup(r => r.DeleteFileAsync("weather/prod/success.json"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _service.DeleteFileAsync("weather/prod/success.json");

        // Assert
        result.DeletedFile.Should().Be("weather/prod/success.json");
        result.DeletedFolders.Should().HaveCount(2);
        result.CommittedToGit.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFileAsync_WhenFileNotFound_PropagatesException()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteFileAsync("nonexistent.json"))
            .ThrowsAsync(new FileNotFoundException("File not found"));

        // Act
        var act = () => _service.DeleteFileAsync("nonexistent.json");

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    #endregion
}
