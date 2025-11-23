using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mockery.Configuration;
using Mockery.Repository;
using Moq;
using Xunit;

namespace Mockery.Test.Repository;

public class GitMockRepositoryTests : IDisposable
{
    private readonly string _testRepoPath;
    private readonly Mock<ILogger<GitMockRepository>> _loggerMock;
    private readonly GitRepositoryOptions _options;

    public GitMockRepositoryTests()
    {
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"mockery-test-{Guid.NewGuid()}");
        _loggerMock = new Mock<ILogger<GitMockRepository>>();

        _options = new GitRepositoryOptions
        {
            ClonePath = _testRepoPath,
            RepositoryUrl = "https://github.com/test/test.git",
            Branch = "main",
            AccessToken = ""
        };

        // Create test directory structure (Git mode uses ClonePath directly, no "mocks" subdirectory)
        Directory.CreateDirectory(_testRepoPath);
        Directory.CreateDirectory(Path.Combine(_testRepoPath, "TestService"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRepoPath))
        {
            Directory.Delete(_testRepoPath, true);
        }
    }

    [Fact]
    public async Task FindMockFileAsync_WhenFileExists_ReturnsContentAndExtension()
    {
        // Arrange
        var mockContent = "{\"test\":\"data\"}";
        var mockFilePath = Path.Combine(_testRepoPath, "TestService", "123.json");
        await File.WriteAllTextAsync(mockFilePath, mockContent);

        var optionsMock = Options.Create(_options);
        var repository = new GitMockRepository(optionsMock, _loggerMock.Object);

        // Manually set initialized flag by calling a test initialization
        // Since we can't call InitializeAsync without Git, we'll use reflection or test the method differently
        // For now, let's simulate initialization by setting up the directory structure
        var initMethod = typeof(GitMockRepository).BaseType!
            .GetField("_initialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        initMethod?.SetValue(repository, true);

        // Act
        var result = await repository.FindMockFileAsync("TestService", "123");

        // Assert
        result.Should().NotBeNull();
        result.Value.Content.Should().Be(mockContent);
        result.Value.Extension.Should().Be(".json");
    }

    [Fact]
    public async Task FindMockFileAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new GitMockRepository(optionsMock, _loggerMock.Object);

        var initMethod = typeof(GitMockRepository).BaseType!
            .GetField("_initialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        initMethod?.SetValue(repository, true);

        // Act
        var result = await repository.FindMockFileAsync("TestService", "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindMockFileAsync_WhenServiceFolderDoesNotExist_ReturnsNull()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new GitMockRepository(optionsMock, _loggerMock.Object);

        var initMethod = typeof(GitMockRepository).BaseType!
            .GetField("_initialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        initMethod?.SetValue(repository, true);

        // Act
        var result = await repository.FindMockFileAsync("NonExistentService", "123");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindMockFileAsync_ExcludesHeadersFiles()
    {
        // Arrange
        var mockContent = "{\"test\":\"data\"}";
        var mockFilePath = Path.Combine(_testRepoPath, "TestService", "456.json");
        var headersFilePath = Path.Combine(_testRepoPath, "TestService", "456.headers.json");

        await File.WriteAllTextAsync(mockFilePath, mockContent);
        await File.WriteAllTextAsync(headersFilePath, "{\"X-Custom\":\"Value\"}");

        var optionsMock = Options.Create(_options);
        var repository = new GitMockRepository(optionsMock, _loggerMock.Object);

        var initMethod = typeof(GitMockRepository).BaseType!
            .GetField("_initialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        initMethod?.SetValue(repository, true);

        // Act
        var result = await repository.FindMockFileAsync("TestService", "456");

        // Assert
        result.Should().NotBeNull();
        result.Value.Extension.Should().Be(".json");
        result.Value.Extension.Should().NotContain("headers");
    }

    [Fact]
    public async Task FindHeadersFileAsync_WhenHeadersExist_ReturnsDictionary()
    {
        // Arrange
        var headersContent = "{\"X-Custom-Header\":\"CustomValue\",\"X-Request-ID\":\"123\"}";
        var headersFilePath = Path.Combine(_testRepoPath, "TestService", "789.headers.json");
        await File.WriteAllTextAsync(headersFilePath, headersContent);

        var optionsMock = Options.Create(_options);
        var repository = new GitMockRepository(optionsMock, _loggerMock.Object);

        var initMethod = typeof(GitMockRepository).BaseType!
            .GetField("_initialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        initMethod?.SetValue(repository, true);

        // Act
        var result = await repository.FindHeadersFileAsync("TestService", "789");

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("X-Custom-Header");
        result!["X-Custom-Header"].Should().Be("CustomValue");
        result.Should().ContainKey("X-Request-ID");
        result["X-Request-ID"].Should().Be("123");
    }

    [Fact]
    public async Task FindHeadersFileAsync_WhenHeadersDoNotExist_ReturnsNull()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new GitMockRepository(optionsMock, _loggerMock.Object);

        var initMethod = typeof(GitMockRepository).BaseType!
            .GetField("_initialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        initMethod?.SetValue(repository, true);

        // Act
        var result = await repository.FindHeadersFileAsync("TestService", "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindMockFileAsync_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new GitMockRepository(optionsMock, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.FindMockFileAsync("TestService", "123"));
    }

    [Fact]
    public async Task FindHeadersFileAsync_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new GitMockRepository(optionsMock, _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.FindHeadersFileAsync("TestService", "123"));
    }
}
