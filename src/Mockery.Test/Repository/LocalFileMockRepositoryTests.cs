using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mockery.Configuration;
using Mockery.Repository;
using Moq;
using Xunit;

namespace Mockery.Test.Repository;

public class LocalFileMockRepositoryTests : IDisposable
{
    private readonly string _testRepoPath;
    private readonly Mock<ILogger<LocalFileMockRepository>> _loggerMock;
    private readonly GitRepositoryOptions _options;

    public LocalFileMockRepositoryTests()
    {
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"mockery-local-test-{Guid.NewGuid()}");
        _loggerMock = new Mock<ILogger<LocalFileMockRepository>>();

        _options = new GitRepositoryOptions
        {
            ClonePath = _testRepoPath,
            RepositoryUrl = "", // Not used in local mode
            Branch = "main",
            AccessToken = ""
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRepoPath))
        {
            Directory.Delete(_testRepoPath, true);
        }
    }

    [Fact]
    public async Task InitializeAsync_CreatesRootMocksDirectory()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);

        // Act
        await repository.InitializeAsync();

        // Assert
        Directory.Exists(Path.Combine(_testRepoPath, "mocks")).Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_WhenDirectoryExists_DoesNotThrow()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_testRepoPath, "mocks"));
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);

        // Act
        Func<Task> act = async () => await repository.InitializeAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InitializeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);

        // Act
        await repository.InitializeAsync();
        await repository.InitializeAsync();
        await repository.InitializeAsync();

        // Assert
        Directory.Exists(Path.Combine(_testRepoPath, "mocks")).Should().BeTrue();
    }

    [Fact]
    public async Task FindMockFileAsync_WhenFileExists_ReturnsContentAndExtension()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "LocalService");
        Directory.CreateDirectory(servicePath);

        var mockContent = "{\"local\":\"test\"}";
        var mockFilePath = Path.Combine(servicePath, "abc.json");
        await File.WriteAllTextAsync(mockFilePath, mockContent);

        // Act
        var result = await repository.FindMockFileAsync("LocalService", "abc");

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
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "LocalService");
        Directory.CreateDirectory(servicePath);

        // Act
        var result = await repository.FindMockFileAsync("LocalService", "nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindMockFileAsync_SupportsMultipleFileTypes()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "MultiTypeService");
        Directory.CreateDirectory(servicePath);

        // Create different file types
        await File.WriteAllTextAsync(Path.Combine(servicePath, "mock1.json"), "{\"type\":\"json\"}");
        await File.WriteAllTextAsync(Path.Combine(servicePath, "mock2.html"), "<html>test</html>");
        await File.WriteAllTextAsync(Path.Combine(servicePath, "mock3.xml"), "<root>test</root>");

        // Act & Assert - JSON
        var jsonResult = await repository.FindMockFileAsync("MultiTypeService", "mock1");
        jsonResult.Should().NotBeNull();
        jsonResult.Value.Extension.Should().Be(".json");

        // Act & Assert - HTML
        var htmlResult = await repository.FindMockFileAsync("MultiTypeService", "mock2");
        htmlResult.Should().NotBeNull();
        htmlResult.Value.Extension.Should().Be(".html");

        // Act & Assert - XML
        var xmlResult = await repository.FindMockFileAsync("MultiTypeService", "mock3");
        xmlResult.Should().NotBeNull();
        xmlResult.Value.Extension.Should().Be(".xml");
    }

    [Fact]
    public async Task FindHeadersFileAsync_WhenHeadersExist_ReturnsDictionary()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "HeaderService");
        Directory.CreateDirectory(servicePath);

        var headersContent = "{\"X-Local-Header\":\"LocalValue\",\"X-Test\":\"Test123\"}";
        var headersFilePath = Path.Combine(servicePath, "test.headers.json");
        await File.WriteAllTextAsync(headersFilePath, headersContent);

        // Act
        var result = await repository.FindHeadersFileAsync("HeaderService", "test");

        // Assert
        result.Should().NotBeNull();
        result.Should().ContainKey("X-Local-Header");
        result!["X-Local-Header"].Should().Be("LocalValue");
        result.Should().ContainKey("X-Test");
        result["X-Test"].Should().Be("Test123");
    }

    [Fact]
    public async Task FindHeadersFileAsync_WhenHeadersDoNotExist_ReturnsNull()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "NoHeaderService");
        Directory.CreateDirectory(servicePath);

        // Act
        var result = await repository.FindHeadersFileAsync("NoHeaderService", "test");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RefreshAsync_DoesNotThrow()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        // Act
        Func<Task> act = async () => await repository.RefreshAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task FindMockFileAsync_FilesAddedAfterInitialization_AreAccessibleImmediately()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "DynamicService");
        Directory.CreateDirectory(servicePath);

        // Act - Add file after initialization
        var mockContent = "{\"dynamic\":\"content\"}";
        var mockFilePath = Path.Combine(servicePath, "new.json");
        await File.WriteAllTextAsync(mockFilePath, mockContent);

        var result = await repository.FindMockFileAsync("DynamicService", "new");

        // Assert
        result.Should().NotBeNull();
        result.Value.Content.Should().Be(mockContent);
    }

    [Fact]
    public async Task FindStatusFileAsync_WhenStatusFileExists_ReturnsStatusCodeAndContent()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "StatusService");
        Directory.CreateDirectory(servicePath);

        var statusContent = "{\"error\":\"Gateway Timeout\"}";
        var statusFilePath = Path.Combine(servicePath, "504.status.json");
        await File.WriteAllTextAsync(statusFilePath, statusContent);

        // Act
        var result = await repository.FindStatusFileAsync("StatusService", "504");

        // Assert
        result.Should().NotBeNull();
        result.Value.StatusCode.Should().Be(504);
        result.Value.Content.Should().Be(statusContent);
    }

    [Fact]
    public async Task FindStatusFileAsync_WhenStatusFileIsEmpty_ReturnsStatusCodeWithNullContent()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "EmptyStatusService");
        Directory.CreateDirectory(servicePath);

        var statusFilePath = Path.Combine(servicePath, "204.status.json");
        await File.WriteAllTextAsync(statusFilePath, "");

        // Act
        var result = await repository.FindStatusFileAsync("EmptyStatusService", "204");

        // Assert
        result.Should().NotBeNull();
        result.Value.StatusCode.Should().Be(204);
        result.Value.Content.Should().BeNull();
    }

    [Fact]
    public async Task FindStatusFileAsync_WhenStatusFileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "NoStatusService");
        Directory.CreateDirectory(servicePath);

        // Act
        var result = await repository.FindStatusFileAsync("NoStatusService", "500");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindStatusFileAsync_WhenFileIdIsNotValidStatusCode_ReturnsNull()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "InvalidStatusService");
        Directory.CreateDirectory(servicePath);

        // Create file with invalid status code in name
        var statusFilePath = Path.Combine(servicePath, "abc.status.json");
        await File.WriteAllTextAsync(statusFilePath, "{}");

        // Act
        var result = await repository.FindStatusFileAsync("InvalidStatusService", "abc");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindStatusFileAsync_WhenStatusCodeOutOfRange_ReturnsNull()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "OutOfRangeService");
        Directory.CreateDirectory(servicePath);

        // Create file with out-of-range status code (600 > 599)
        var statusFilePath = Path.Combine(servicePath, "600.status.json");
        await File.WriteAllTextAsync(statusFilePath, "{}");

        // Act
        var result = await repository.FindStatusFileAsync("OutOfRangeService", "600");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindMockFileAsync_ExcludesStatusJsonFiles()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "StatusExclusionService");
        Directory.CreateDirectory(servicePath);

        // Create only a .status.json file
        var statusFilePath = Path.Combine(servicePath, "500.status.json");
        await File.WriteAllTextAsync(statusFilePath, "{\"error\":\"test\"}");

        // Act
        var result = await repository.FindMockFileAsync("StatusExclusionService", "500");

        // Assert
        result.Should().BeNull(); // .status.json should be excluded from regular mock file search
    }

    [Fact]
    public async Task FindMockFileAsync_WhenBothRegularAndStatusFileExist_ReturnsRegularFile()
    {
        // Arrange
        var optionsMock = Options.Create(_options);
        var repository = new LocalFileMockRepository(optionsMock, _loggerMock.Object);
        await repository.InitializeAsync();

        var servicePath = Path.Combine(_testRepoPath, "mocks", "BothFilesService");
        Directory.CreateDirectory(servicePath);

        // Create both a regular .json file and a .status.json file
        var regularFilePath = Path.Combine(servicePath, "500.json");
        await File.WriteAllTextAsync(regularFilePath, "{\"regular\":\"content\"}");

        var statusFilePath = Path.Combine(servicePath, "500.status.json");
        await File.WriteAllTextAsync(statusFilePath, "{\"status\":\"content\"}");

        // Act
        var mockResult = await repository.FindMockFileAsync("BothFilesService", "500");
        var statusResult = await repository.FindStatusFileAsync("BothFilesService", "500");

        // Assert
        mockResult.Should().NotBeNull();
        mockResult.Value.Content.Should().Be("{\"regular\":\"content\"}");
        
        statusResult.Should().NotBeNull();
        statusResult.Value.Content.Should().Be("{\"status\":\"content\"}");
    }
}
