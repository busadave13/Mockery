namespace Mockery.Repository;

public interface IGitMockRepository
{
    Task InitializeAsync();
    Task<(string Content, string Extension)?> FindMockFileAsync(string serviceName, string fileId);
    Task<Dictionary<string, string>?> FindHeadersFileAsync(string serviceName, string fileId);
    Task RefreshAsync();
}
