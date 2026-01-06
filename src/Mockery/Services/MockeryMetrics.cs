using System.Diagnostics.Metrics;
using Mockery.Configuration;
using Microsoft.Extensions.Options;

namespace Mockery.Services;

/// <summary>
/// Provides custom OpenTelemetry metrics for Mockery.
/// </summary>
public class MockeryMetrics
{
    private readonly Counter<long> _mocksServedCounter;
    private readonly Counter<long> _throttledRequestsCounter;
    private readonly Counter<long> _totalRequestsCounter;
    private readonly IOptionsMonitor<ThrottlingOptions> _throttlingOptions;
    private readonly IThrottlingService _throttlingService;

    public MockeryMetrics(
        IMeterFactory meterFactory,
        IOptionsMonitor<ThrottlingOptions> throttlingOptions,
        IThrottlingService throttlingService)
    {
        _throttlingOptions = throttlingOptions ?? throw new ArgumentNullException(nameof(throttlingOptions));
        _throttlingService = throttlingService ?? throw new ArgumentNullException(nameof(throttlingService));
        
        var meter = meterFactory.Create("Mockery");
        
        // Counters
        _mocksServedCounter = meter.CreateCounter<long>(
            "mockery.mocks.served",
            unit: "{mocks}",
            description: "Number of mocks served by the API");
        
        _throttledRequestsCounter = meter.CreateCounter<long>(
            "mockery.requests.throttled",
            unit: "{requests}",
            description: "Number of requests throttled by rate limiting");
        
        _totalRequestsCounter = meter.CreateCounter<long>(
            "mockery.requests.total",
            unit: "{requests}",
            description: "Total number of requests received");
        
        // Gauges for throttling configuration and state
        meter.CreateObservableGauge(
            "mockery.throttling.enabled",
            () => _throttlingOptions.CurrentValue.Enabled ? 1 : 0,
            unit: "{boolean}",
            description: "Whether throttling is enabled (1) or disabled (0)");
        
        meter.CreateObservableGauge(
            "mockery.throttling.rate_limit",
            () => _throttlingOptions.CurrentValue.RequestsPerSecond,
            unit: "{requests}/s",
            description: "Configured rate limit in requests per second");
        
        meter.CreateObservableGauge(
            "mockery.throttling.burst_size",
            () => _throttlingOptions.CurrentValue.BurstSize,
            unit: "{requests}",
            description: "Configured burst size (token bucket capacity)");
        
        meter.CreateObservableGauge(
            "mockery.throttling.tokens_available",
            () => _throttlingService.AvailableTokens,
            unit: "{tokens}",
            description: "Current number of tokens available in the bucket");
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

    /// <summary>
    /// Records that a request was throttled due to rate limiting.
    /// </summary>
    public void IncrementThrottledRequests()
    {
        _throttledRequestsCounter.Add(1);
    }
    
    /// <summary>
    /// Records that a request was received (regardless of outcome).
    /// </summary>
    public void IncrementTotalRequests()
    {
        _totalRequestsCounter.Add(1);
    }
}
