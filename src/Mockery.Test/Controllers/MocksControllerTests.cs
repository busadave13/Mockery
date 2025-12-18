using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mockery.BusinessLogic;
using Mockery.Controllers;
using Mockery.Models;
using Moq;
using System.Text;
using Xunit;

namespace Mockery.Test.Controllers;

public class MocksControllerTests
{
    private readonly Mock<IMocksManagementService> _mockService;
    private readonly Mock<ILogger<MocksController>> _mockLogger;
    private readonly MocksController _controller;

    public MocksControllerTests()
    {
        _mockService = new Mock<IMocksManagementService>();
        _mockLogger = new Mock<ILogger<MocksController>>();
        _controller = new MocksController(_mockService.Object, _mockLogger.Object);
    }

    private void SetupHttpContext(string? headerValue = null, string? body = null)
    {
        var httpContext = new DefaultHttpContext();
        
        if (headerValue != null)
        {
            httpContext.Request.Headers["X-Mockery-Mock"] = headerValue;
        }
        
        if (body != null)
        {
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        }
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region ListDirectory Tests

    [Fact]
    public async Task ListDirectory_WithNoHeader_ReturnsRootListing()
    {
        // Arrange
        SetupHttpContext();
        var expectedResponse = new DirectoryListingResponse
        {
            Path = "/",
            Items = new List<DirectoryItem>
            {
                new() { Name = "FooBar", Type = "folder" },
                new() { Name = "Products", Type = "folder" }
            }
        };
        _mockService.Setup(s => s.ListDirectoryAsync(string.Empty))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ListDirectory();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<DirectoryListingResponse>().Subject;
        response.Path.Should().Be("/");
        response.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListDirectory_WithPath_ReturnsDirectoryListing()
    {
        // Arrange
        SetupHttpContext("weather/prod");
        var expectedResponse = new DirectoryListingResponse
        {
            Path = "weather/prod",
            Items = new List<DirectoryItem>
            {
                new() { Name = "success.json", Type = "file", Extension = ".json", Size = 42 }
            }
        };
        _mockService.Setup(s => s.ListDirectoryAsync("weather/prod"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ListDirectory();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<DirectoryListingResponse>().Subject;
        response.Path.Should().Be("weather/prod");
        response.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListDirectory_WithLeadingSlashes_PassesPathToService()
    {
        // Arrange
        SetupHttpContext("//weather");
        var expectedResponse = new DirectoryListingResponse
        {
            Path = "weather",
            Items = new List<DirectoryItem>()
        };
        _mockService.Setup(s => s.ListDirectoryAsync("//weather"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ListDirectory();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _mockService.Verify(s => s.ListDirectoryAsync("//weather"), Times.Once);
    }

    #endregion

    #region CreateMock Tests

    [Fact]
    public async Task CreateMock_WithValidRequest_Returns201Created()
    {
        // Arrange
        var content = "{\"temp\": 72}";
        SetupHttpContext("weather/prod/success.json", content);
        var expectedResponse = new CreateMockResponse
        {
            Path = "weather/prod",
            FileName = "success.json",
            Size = 15,
            CommittedToGit = false
        };
        _mockService.Setup(s => s.CreateFileAsync("weather/prod/success.json", content))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateMock();

        // Assert
        var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        var response = createdResult.Value.Should().BeOfType<CreateMockResponse>().Subject;
        response.FileName.Should().Be("success.json");
        response.Path.Should().Be("weather/prod");
    }

    [Fact]
    public async Task CreateMock_WithMissingHeader_Returns400BadRequest()
    {
        // Arrange
        SetupHttpContext(null, "{\"temp\": 72}");

        // Act
        var result = await _controller.CreateMock();

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateMock_WithEmptyBody_Returns400BadRequest()
    {
        // Arrange
        SetupHttpContext("weather/prod/success.json", "");

        // Act
        var result = await _controller.CreateMock();

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateMock_WithGitMode_ReturnsCommittedTrue()
    {
        // Arrange
        var content = "{\"temp\": 72}";
        SetupHttpContext("weather/prod/success.json", content);
        var expectedResponse = new CreateMockResponse
        {
            Path = "weather/prod",
            FileName = "success.json",
            Size = 15,
            CommittedToGit = true
        };
        _mockService.Setup(s => s.CreateFileAsync("weather/prod/success.json", content))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CreateMock();

        // Assert
        var createdResult = result.Should().BeOfType<ObjectResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<CreateMockResponse>().Subject;
        response.CommittedToGit.Should().BeTrue();
    }

    #endregion

    #region DeleteMock Tests

    [Fact]
    public async Task DeleteMock_WithValidPath_Returns200Ok()
    {
        // Arrange
        SetupHttpContext("weather/prod/success.json");
        var expectedResponse = new DeleteMockResponse
        {
            DeletedFile = "weather/prod/success.json",
            DeletedFolders = new List<string>(),
            CommittedToGit = false
        };
        _mockService.Setup(s => s.DeleteFileAsync("weather/prod/success.json"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeleteMock();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<DeleteMockResponse>().Subject;
        response.DeletedFile.Should().Be("weather/prod/success.json");
    }

    [Fact]
    public async Task DeleteMock_WithMissingHeader_Returns400BadRequest()
    {
        // Arrange
        SetupHttpContext();

        // Act
        var result = await _controller.DeleteMock();

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteMock_WithNonExistentFile_Returns404NotFound()
    {
        // Arrange
        SetupHttpContext("weather/prod/notfound.json");
        _mockService.Setup(s => s.DeleteFileAsync("weather/prod/notfound.json"))
            .ThrowsAsync(new FileNotFoundException("File not found"));

        // Act
        var result = await _controller.DeleteMock();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteMock_WithDeletedFolders_ReturnsFolderList()
    {
        // Arrange
        SetupHttpContext("weather/prod/success.json");
        var expectedResponse = new DeleteMockResponse
        {
            DeletedFile = "weather/prod/success.json",
            DeletedFolders = new List<string> { "weather/prod", "weather" },
            CommittedToGit = true
        };
        _mockService.Setup(s => s.DeleteFileAsync("weather/prod/success.json"))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.DeleteMock();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<DeleteMockResponse>().Subject;
        response.DeletedFolders.Should().HaveCount(2);
        response.DeletedFolders.Should().Contain("weather/prod");
        response.DeletedFolders.Should().Contain("weather");
    }

    #endregion
}
