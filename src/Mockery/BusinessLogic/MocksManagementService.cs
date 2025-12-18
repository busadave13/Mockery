using Mockery.Models;
using Mockery.Repository;

namespace Mockery.BusinessLogic;

/// <summary>
/// Service for managing mock files (list, create, delete).
/// </summary>
public class MocksManagementService : IMocksManagementService
{
    private readonly IGitMockRepository _repository;
    private readonly ILogger<MocksManagementService> _logger;

    public MocksManagementService(IGitMockRepository repository, ILogger<MocksManagementService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DirectoryListingResponse> ListDirectoryAsync(string path)
    {
        _logger.LogInformation("Listing directory: {Path}", path);
        return await _repository.ListDirectoryAsync(path);
    }

    public async Task<CreateMockResponse> CreateFileAsync(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        ArgumentNullException.ThrowIfNull(content, nameof(content));
        
        _logger.LogInformation("Creating mock file: {Path}", path);
        return await _repository.CreateFileAsync(path, content);
    }

    public async Task<DeleteMockResponse> DeleteFileAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        
        _logger.LogInformation("Deleting mock file: {Path}", path);
        return await _repository.DeleteFileAsync(path);
    }
}
