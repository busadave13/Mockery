using Microsoft.AspNetCore.Mvc;
using Mockery.BusinessLogic;
using Mockery.Models;

namespace Mockery.Controllers;

/// <summary>
/// Controller for managing mock files (list, create, delete).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MocksController : ControllerBase
{
    private const string MockHeaderName = "X-Mockery-Mock";
    
    private readonly IMocksManagementService _mocksManagementService;
    private readonly ILogger<MocksController> _logger;

    public MocksController(IMocksManagementService mocksManagementService, ILogger<MocksController> logger)
    {
        _mocksManagementService = mocksManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Lists the contents of a directory at the specified path.
    /// </summary>
    /// <remarks>
    /// Use the X-Mockery-Mock header to specify the path:
    /// - Empty or "/" for root directory
    /// - "weather" for weather folder
    /// - "weather/prod" for nested folder
    /// </remarks>
    /// <returns>Directory listing containing folders and files.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(DirectoryListingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListDirectory()
    {
        // Get path from header (optional - empty means root)
        var path = Request.Headers[MockHeaderName].FirstOrDefault() ?? string.Empty;
        
        _logger.LogInformation("Listing directory: {Path}", path);
        
        var result = await _mocksManagementService.ListDirectoryAsync(path);
        return Ok(result);
    }

    /// <summary>
    /// Creates a new mock file at the specified path.
    /// </summary>
    /// <remarks>
    /// Use the X-Mockery-Mock header to specify the full path including filename:
    /// - "weather/prod/success.json"
    /// - "weather/prod/success.headers.json"
    /// 
    /// The request body contains the file content.
    /// For Git mode, changes are automatically committed and pushed.
    /// </remarks>
    /// <returns>Information about the created file.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateMockResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateMock()
    {
        // Get path from header (required)
        var path = Request.Headers[MockHeaderName].FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Missing {HeaderName} header", MockHeaderName);
            return BadRequest(new { error = $"Missing {MockHeaderName} header. Specify the full path including filename." });
        }
        
        // Read content from request body
        string content;
        using (var reader = new StreamReader(Request.Body))
        {
            content = await reader.ReadToEndAsync();
        }
        
        if (string.IsNullOrEmpty(content))
        {
            _logger.LogWarning("Empty request body for file creation");
            return BadRequest(new { error = "Request body cannot be empty. Provide the file content." });
        }
        
        _logger.LogInformation("Creating mock file: {Path}", path);
        
        try
        {
            var result = await _mocksManagementService.CreateFileAsync(path, content);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            _logger.LogWarning(ex, "File already exists: {Path}", path);
            return Conflict(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid path for file creation: {Path}", path);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a mock file at the specified path.
    /// </summary>
    /// <remarks>
    /// Use the X-Mockery-Mock header to specify the full path including filename:
    /// - "weather/prod/success.json"
    /// 
    /// If the folder becomes empty after deletion, it will also be deleted.
    /// For Git mode, changes are automatically committed and pushed.
    /// </remarks>
    /// <returns>Information about the deleted file and folders.</returns>
    [HttpDelete]
    [ProducesResponseType(typeof(DeleteMockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMock()
    {
        // Get path from header (required)
        var path = Request.Headers[MockHeaderName].FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Missing {HeaderName} header", MockHeaderName);
            return BadRequest(new { error = $"Missing {MockHeaderName} header. Specify the full path including filename." });
        }
        
        _logger.LogInformation("Deleting mock file: {Path}", path);
        
        try
        {
            var result = await _mocksManagementService.DeleteFileAsync(path);
            return Ok(result);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "File not found for deletion: {Path}", path);
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid path for file deletion: {Path}", path);
            return BadRequest(new { error = ex.Message });
        }
    }
}
