using Microsoft.AspNetCore.Mvc;
using Mockery.BusinessLogic;

namespace Mockery.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MockController : ControllerBase
{
    private readonly IMockService _mockService;
    private readonly ILogger<MockController> _logger;

    public MockController(IMockService mockService, ILogger<MockController> logger)
    {
        _mockService = mockService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetMock()
    {
        // Extract X-Mockery-Mock header
        if (!Request.Headers.TryGetValue("X-Mockery-Mock", out var mockIdHeader) || string.IsNullOrWhiteSpace(mockIdHeader))
        {
            _logger.LogWarning("X-Mockery-Mock header is missing or empty");
            return BadRequest(new { error = "Missing X-Mockery-Mock header" });
        }

        // Parse comma-separated mock IDs
        var mockIds = mockIdHeader.ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(id => id.Trim())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToList();

        if (!mockIds.Any())
        {
            _logger.LogWarning("X-Mockery-Mock header contains no valid mock IDs");
            return BadRequest(new { error = "X-Mockery-Mock header contains no valid mock IDs" });
        }

        try
        {
            var result = await _mockService.GetMockAsync(mockIds);

            if (result == null)
            {
                var mockIdsString = string.Join(", ", mockIds);
                _logger.LogWarning("Mock not found for IDs: {MockIds}", mockIdsString);
                return NotFound(new
                {
                    error = "Mock not found",
                    mockIds = mockIds
                });
            }

            // Set custom headers
            foreach (var header in result.CustomHeaders)
            {
                Response.Headers[header.Key] = header.Value;
            }

            // Set status code from .status.json file or default to 200
            var responseStatusCode = result.StatusCode ?? 200;
            Response.StatusCode = responseStatusCode;

            // Return content based on status code behavior
            if (result.ShouldReturnContent && !string.IsNullOrEmpty(result.Content))
            {
                _logger.LogInformation("Returning mock content with status {StatusCode}", responseStatusCode);
                return Content(result.Content, result.ContentType);
            }
            else
            {
                // No content (204, 404, or empty content)
                _logger.LogInformation("Returning empty response with status {StatusCode}", responseStatusCode);
                return new EmptyResult();
            }
        }
        catch (Exception ex)
        {
            var mockIdsString = string.Join(", ", mockIds);
            _logger.LogError(ex, "Error retrieving mock for IDs: {MockIds}", mockIdsString);
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}
