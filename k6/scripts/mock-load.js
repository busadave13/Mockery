import http from 'k6/http';
import { check, sleep } from 'k6';
import { Rate, Counter, Trend } from 'k6/metrics';
import {
    getConfig,
    thresholds,
    constantArrivalRateScenario,
    rampingArrivalRateScenario,
    mockHeaders,
} from '../config.js';

// Custom metrics
const mockServedRate = new Rate('mock_served_success');
const mockNotFoundRate = new Rate('mock_not_found');
const mockRequests = new Counter('mock_requests_total');
const mockLatency = new Trend('mock_latency', true);

// Configuration
const config = getConfig();
const scenarioType = __ENV.SCENARIO || 'constant'; // 'constant' or 'ramping'

// Dynamic scenario configuration based on environment
export const options = {
    scenarios: {
        mock_load: scenarioType === 'ramping'
            ? rampingArrivalRateScenario(config.rps, config.duration)
            : constantArrivalRateScenario(config.rps, config.duration, config.vus),
    },
    thresholds: {
        ...thresholds,
        mock_served_success: ['rate>0.99'], // 99% of mocks should be served successfully
        mock_latency: ['p(95)<500'],        // 95% of mock requests under 500ms
    },
};

/**
 * Setup function - runs once before the test
 */
export function setup() {
    console.log('='.repeat(60));
    console.log('Mockery Load Test - GET /api/mock');
    console.log('='.repeat(60));
    console.log(`Target URL: ${config.baseUrl}`);
    console.log(`Mock ID: ${config.mockId}`);
    console.log(`Target RPS: ${config.rps}`);
    console.log(`Duration: ${config.duration}`);
    console.log(`Scenario: ${scenarioType}`);
    console.log('='.repeat(60));

    // Verify the mock endpoint is accessible
    const warmupResponse = http.get(
        `${config.baseUrl}/api/mock`,
        { headers: mockHeaders(config.mockId) }
    );

    if (warmupResponse.status !== 200) {
        console.warn(`Warning: Warmup request returned status ${warmupResponse.status}`);
        console.warn(`Response: ${warmupResponse.body}`);
    }

    return { config };
}

/**
 * Main test function - executed by each VU iteration
 */
export default function (data) {
    const url = `${data.config.baseUrl}/api/mock`;
    const headers = mockHeaders(data.config.mockId);

    const startTime = Date.now();
    const response = http.get(url, { headers });
    const latency = Date.now() - startTime;

    // Record custom metrics
    mockRequests.add(1);
    mockLatency.add(latency);

    // Check response
    const success = check(response, {
        'status is 200': (r) => r.status === 200,
        'response has content': (r) => r.body && r.body.length > 0,
        'content-type is set': (r) => r.headers['Content-Type'] !== undefined,
        'response time < 500ms': (r) => r.timings.duration < 500,
    });

    // Track success/failure rates
    mockServedRate.add(response.status === 200);
    mockNotFoundRate.add(response.status === 404);

    // Optional: Add a small think time between requests for more realistic simulation
    // Uncomment if needed for your use case
    // sleep(0.1);
}

/**
 * Teardown function - runs once after the test
 */
export function teardown(data) {
    console.log('='.repeat(60));
    console.log('Load test completed');
    console.log('='.repeat(60));
}

/**
 * Handle summary - custom summary output
 */
export function handleSummary(data) {
    const summary = {
        timestamp: new Date().toISOString(),
        config: config,
        metrics: {
            http_reqs: data.metrics.http_reqs?.values?.count || 0,
            http_req_duration_avg: data.metrics.http_req_duration?.values?.avg || 0,
            http_req_duration_p95: data.metrics.http_req_duration?.values['p(95)'] || 0,
            http_req_failed_rate: data.metrics.http_req_failed?.values?.rate || 0,
            mock_served_success_rate: data.metrics.mock_served_success?.values?.rate || 0,
        },
        thresholds_passed: Object.keys(data.thresholds || {}).every(
            (key) => data.thresholds[key].ok
        ),
    };

    return {
        stdout: JSON.stringify(summary, null, 2) + '\n',
        'k6/results/mock-load-summary.json': JSON.stringify(summary, null, 2),
    };
}
