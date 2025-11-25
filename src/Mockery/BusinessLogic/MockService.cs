using Mockery.Models;
using Mockery.Repository;
using Mockery.Services;

namespace Mockery.BusinessLogic;

public class MockService : IMockService
{
    private readonly IGitMockRepository _repository;
    private readonly IContentTypeResolver _contentTypeResolver;
    private readonly ILogger<MockService> _logger;

    public MockService(
        IGitMockRepository repository,
        IContentTypeResolver contentTypeResolver,
        ILogger<MockService> logger)
    {
        _repository = repository;
        _contentTypeResolver = contentTypeResolver;
        _logger = logger;
    }

    public async Task<MockFileResult?> GetMockAsync(IEnumerable<string> mockIds)
    {
        var mockIdList = mockIds.ToList();
        if (!mockIdList.Any())
        {
            _logger.LogWarning("No mock IDs provided");
            return null;
        }

        // Random selection if multiple IDs
        var selectedMockId = mockIdList.Count > 1
            ? mockIdList[Random.Shared.Next(mockIdList.Count)]
            : mockIdList[0];

        _logger.LogInformation("Selected mock ID: {MockId}", selectedMockId);

        // Parse mock ID to extract service name and file ID
        var parts = selectedMockId.Split('/', 2);
        if (parts.Length != 2)
        {
            _logger.LogWarning("Invalid mock ID format: {MockId}. Expected format: ServiceName/FileId", selectedMockId);
            return null;
        }

        var serviceName = parts[0];
        var fileId = parts[1];

        var result = new MockFileResult();

        // Always try to get headers file (if it exists)
        var headers = await _repository.FindHeadersFileAsync(serviceName, fileId);
        if (headers != null)
        {
            result.CustomHeaders = headers;
            _logger.LogInformation("Found {Count} custom headers for {MockId}", headers.Count, selectedMockId);
        }

        // First, check for a .status.json file (e.g., 504.status.json)
        var statusFile = await _repository.FindStatusFileAsync(serviceName, fileId);
        if (statusFile != null)
        {
            // Status file found - use status code from filename
            result.StatusCode = statusFile.Value.StatusCode;
            result.ShouldReturnContent = ShouldReturnContent(statusFile.Value.StatusCode);

            if (result.ShouldReturnContent && !string.IsNullOrEmpty(statusFile.Value.Content))
            {
                result.Content = statusFile.Value.Content;
                result.ContentType = _contentTypeResolver.GetContentType(".json");
            }

            _logger.LogInformation("Using status file: {ServiceName}/{FileId}.status.json, StatusCode: {StatusCode}",
                serviceName, fileId, statusFile.Value.StatusCode);

            return result;
        }

        // No status file found - fall back to regular mock file lookup
        result.ShouldReturnContent = true;

        var mockFile = await _repository.FindMockFileAsync(serviceName, fileId);
        if (mockFile == null)
        {
            _logger.LogWarning("Mock file not found: {ServiceName}/{FileId}", serviceName, fileId);
            return null;
        }

        result.Content = mockFile.Value.Content;
        result.ContentType = _contentTypeResolver.GetContentType(mockFile.Value.Extension);

        _logger.LogInformation("Retrieved mock file: {ServiceName}/{FileId}, ContentType: {ContentType}",
            serviceName, fileId, result.ContentType);

        return result;
    }

    private static bool ShouldReturnContent(int statusCode)
    {
        // Status code semantics - some status codes should not return content
        return statusCode switch
        {
            204 => false, // No Content - by definition
            _ => true     // All other status codes can return content
        };
    }
}
