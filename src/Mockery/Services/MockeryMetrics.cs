using System.Diagnostics.Metrics;

namespace Mockery.Services;

/// <summary>
/// Provides custom OpenTelemetry metrics for Mockery.
/// </summary>
public class MockeryMetrics
{
    private readonly Counter<long> _mocksServedCounter;

    public MockeryMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Mockery");
        _mocksServedCounter = meter.CreateCounter<long>(
            "mockery.mocks.served",
            unit: "{mocks}",
            description: "Number of mocks served by the API");
    }

    /// <summary>
    /// Records that a mock was served by the API.
    /// </summary>
    /// <param name="mockId">The mock ID that was served</param>
    /// <param name="statusCode">The HTTP status code returned</param>
    public void RecordMockServed(string mockId, int statusCode)
    {
        _mocksServedCounter.Add(1,
            new KeyValuePair<string, object?>("mock.id", mockId),
            new KeyValuePair<string, object?>("http.status_code", statusCode));
    }
}
