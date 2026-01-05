import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Rate, Counter, Trend } from 'k6/metrics';
import {
    getConfig,
    thresholds,
    stressThresholds,
    calculateVUs,
    mockHeaders,
    defaultHeaders,
} from '../config.js';

// Custom metrics per scenario
const mockGetSuccess = new Rate('mock_get_success');
const mockListSuccess = new Rate('mock_list_success');
const mockGetLatency = new Trend('mock_get_latency', true);
const mockListLatency = new Trend('mock_list_latency', true);
const totalRequests = new Counter('total_requests');

// Configuration
const config = getConfig();
const testMode = __ENV.TEST_MODE || 'mixed'; // 'mixed', 'read-heavy', 'stress'

// Traffic distribution ratios
const trafficPatterns = {
    mixed: { getMock: 0.7, listMocks: 0.3 },           // 70% get, 30% list
    'read-heavy': { getMock: 0.95, listMocks: 0.05 }, // 95% get, 5% list
    stress: { getMock: 0.8, listMocks: 0.2 },          // 80% get, 20% list
};

const pattern = trafficPatterns[testMode] || trafficPatterns.mixed;

// Calculate VUs for the total RPS
const totalVUs = config.vus || calculateVUs(config.rps);

// Parse duration for stages
function parseDuration(duration) {
    const match = duration.match(/^(\d+)(s|m|h)$/);
    if (!match) return 60;
    const value = parseInt(match[1]);
    switch (match[2]) {
        case 's': return value;
        case 'm': return value * 60;
        case 'h': return value * 3600;
        default: return 60;
    }
}

const totalSeconds = parseDuration(config.duration);

// Build scenarios based on test mode
function buildScenarios() {
    if (testMode === 'stress') {
        // Stress test: ramp up to 3x target RPS
        return {
            stress_ramp: {
                executor: 'ramping-arrival-rate',
                startRate: 0,
                timeUnit: '1s',
                preAllocatedVUs: totalVUs * 2,
                maxVUs: totalVUs * 4,
                stages: [
                    { duration: `${Math.ceil(totalSeconds * 0.2)}s`, target: config.rps },
                    { duration: `${Math.ceil(totalSeconds * 0.3)}s`, target: config.rps * 2 },
                    { duration: `${Math.ceil(totalSeconds * 0.3)}s`, target: config.rps * 3 },
                    { duration: `${Math.ceil(totalSeconds * 0.2)}s`, target: 0 },
                ],
                exec: 'mixedWorkload',
            },
        };
    }

    // Default: constant rate for mixed/read-heavy
    return {
        constant_load: {
            executor: 'constant-arrival-rate',
            rate: config.rps,
            timeUnit: '1s',
            duration: config.duration,
            preAllocatedVUs: totalVUs,
            maxVUs: totalVUs * 2,
            exec: 'mixedWorkload',
        },
    };
}

// Dynamic options based on test mode
export const options = {
    scenarios: buildScenarios(),
    thresholds: testMode === 'stress' ? {
        ...stressThresholds,
        mock_get_success: ['rate>0.95'],
        mock_list_success: ['rate>0.95'],
    } : {
        ...thresholds,
        mock_get_success: ['rate>0.99'],
        mock_list_success: ['rate>0.99'],
        mock_get_latency: ['p(95)<500'],
        mock_list_latency: ['p(95)<500'],
    },
};

/**
 * Setup function - runs once before the test
 */
export function setup() {
    console.log('='.repeat(60));
    console.log('Mockery Full Suite Load Test');
    console.log('='.repeat(60));
    console.log(`Target URL: ${config.baseUrl}`);
    console.log(`Mock ID: ${config.mockId}`);
    console.log(`Target RPS: ${config.rps}`);
    console.log(`Duration: ${config.duration}`);
    console.log(`Test Mode: ${testMode}`);
    console.log(`Traffic Pattern: GET ${pattern.getMock * 100}%, LIST ${pattern.listMocks * 100}%`);
    console.log(`VUs: ${totalVUs}`);
    console.log('='.repeat(60));

    // Verify endpoints are accessible
    console.log('Running warmup checks...');
    
    // Check mock endpoint
    const mockResponse = http.get(
        `${config.baseUrl}/api/mock`,
        { headers: mockHeaders(config.mockId) }
    );
    console.log(`  GET /api/mock: ${mockResponse.status}`);

    // Check mocks list endpoint
    const listResponse = http.get(
        `${config.baseUrl}/api/mocks`,
        { headers: defaultHeaders }
    );
    console.log(`  GET /api/mocks: ${listResponse.status}`);

    console.log('='.repeat(60));

    return { config, pattern };
}

/**
 * Mixed workload - probabilistically chooses between operations
 */
export function mixedWorkload(data) {
    totalRequests.add(1);
    const rand = Math.random();

    if (rand < data.pattern.getMock) {
        // GET mock endpoint
        getMock(data);
    } else {
        // LIST mocks endpoint
        listMocks(data);
    }
}

/**
 * GET /api/mock - Retrieve a mock
 */
function getMock(data) {
    const url = `${data.config.baseUrl}/api/mock`;
    const headers = mockHeaders(data.config.mockId);

    const startTime = Date.now();
    const response = http.get(url, { headers });
    const latency = Date.now() - startTime;

    mockGetLatency.add(latency);

    const success = check(response, {
        '[GET] status is 200': (r) => r.status === 200,
        '[GET] has content': (r) => r.body && r.body.length > 0,
        '[GET] response time < 500ms': (r) => r.timings.duration < 500,
    });

    mockGetSuccess.add(response.status === 200);
}

/**
 * GET /api/mocks - List directory
 */
function listMocks(data) {
    const url = `${data.config.baseUrl}/api/mocks`;
    const headers = defaultHeaders;

    const startTime = Date.now();
    const response = http.get(url, { headers });
    const latency = Date.now() - startTime;

    mockListLatency.add(latency);

    const success = check(response, {
        '[LIST] status is 200': (r) => r.status === 200,
        '[LIST] is valid JSON': (r) => {
            try {
                JSON.parse(r.body);
                return true;
            } catch (e) {
                return false;
            }
        },
        '[LIST] response time < 500ms': (r) => r.timings.duration < 500,
    });

    mockListSuccess.add(response.status === 200);
}

/**
 * Teardown function - runs once after the test
 */
export function teardown(data) {
    console.log('='.repeat(60));
    console.log('Full suite load test completed');
    console.log('='.repeat(60));
}

/**
 * Handle summary - custom summary output
 */
export function handleSummary(data) {
    const summary = {
        timestamp: new Date().toISOString(),
        testMode: testMode,
        config: config,
        trafficPattern: pattern,
        metrics: {
            total_requests: data.metrics.total_requests?.values?.count || 0,
            http_reqs: data.metrics.http_reqs?.values?.count || 0,
            http_req_duration_avg: data.metrics.http_req_duration?.values?.avg || 0,
            http_req_duration_p50: data.metrics.http_req_duration?.values['p(50)'] || 0,
            http_req_duration_p90: data.metrics.http_req_duration?.values['p(90)'] || 0,
            http_req_duration_p95: data.metrics.http_req_duration?.values['p(95)'] || 0,
            http_req_failed_rate: data.metrics.http_req_failed?.values?.rate || 0,
            mock_get_success_rate: data.metrics.mock_get_success?.values?.rate || 0,
            mock_list_success_rate: data.metrics.mock_list_success?.values?.rate || 0,
            mock_get_latency_p95: data.metrics.mock_get_latency?.values?.['p(95)'] || 0,
            mock_list_latency_p95: data.metrics.mock_list_latency?.values?.['p(95)'] || 0,
        },
        thresholds_passed: Object.keys(data.thresholds || {}).every(
            (key) => data.thresholds[key].ok
        ),
        thresholds_details: Object.entries(data.thresholds || {}).map(([name, result]) => ({
            name,
            passed: result.ok,
        })),
    };

    return {
        stdout: JSON.stringify(summary, null, 2) + '\n',
        'k6/results/full-suite-summary.json': JSON.stringify(summary, null, 2),
    };
}
