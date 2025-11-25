using Mockery.Models;

namespace Mockery.BusinessLogic;

public interface IMockService
{
    Task<MockFileResult?> GetMockAsync(IEnumerable<string> mockIds);
}
