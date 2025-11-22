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

    public async Task<MockFileResult?> GetMockAsync(IEnumerable<string> mockIds, int? statusCode = null)
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

        // Determine if we should return content based on status code semantics
        var shouldReturnContent = ShouldReturnContent(statusCode);

        var result = new MockFileResult
        {
            ShouldReturnContent = shouldReturnContent
        };

        // Always try to get headers file (if it exists)
        var headers = await _repository.FindHeadersFileAsync(serviceName, fileId);
        if (headers != null)
        {
            result.CustomHeaders = headers;
            _logger.LogInformation("Found {Count} custom headers for {MockId}", headers.Count, selectedMockId);
        }

        // Only retrieve mock file content if status code semantics allow it
        if (shouldReturnContent)
        {
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
        }
        else
        {
            _logger.LogInformation("Status code {StatusCode} - skipping content retrieval", statusCode);
        }

        return result;
    }

    private bool ShouldReturnContent(int? statusCode)
    {
        if (!statusCode.HasValue)
        {
            return true; // Default: return content
        }

        // Status code semantics - only return content for 2xx and 3xx status codes
        return statusCode.Value switch
        {
            >= 200 and < 400 => true,  // 2xx Success and 3xx Redirect - return content
            _ => false                  // 4xx Client Error and 5xx Server Error - no content
        };
    }
}
