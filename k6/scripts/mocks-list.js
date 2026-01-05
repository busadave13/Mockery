import http from 'k6/http';
import { check } from 'k6';
import { Rate, Counter, Trend } from 'k6/metrics';
import {
    getConfig,
    thresholds,
    constantArrivalRateScenario,
    rampingArrivalRateScenario,
    mockHeaders,
    defaultHeaders,
} from '../config.js';

// Custom metrics
const listSuccessRate = new Rate('list_success');
const listRequests = new Counter('list_requests_total');
const listLatency = new Trend('list_latency', true);

// Configuration
const config = getConfig();
const scenarioType = __ENV.SCENARIO || 'constant'; // 'constant' or 'ramping'
const listPath = __ENV.LIST_PATH || ''; // Path to list, empty for root

// Dynamic scenario configuration based on environment
export const options = {
    scenarios: {
        mocks_list: scenarioType === 'ramping'
            ? rampingArrivalRateScenario(config.rps, config.duration)
            : constantArrivalRateScenario(config.rps, config.duration, config.vus),
    },
    thresholds: {
        ...thresholds,
        list_success: ['rate>0.99'],   // 99% of list requests should succeed
        list_latency: ['p(95)<500'],   // 95% of list requests under 500ms
    },
};

/**
 * Setup function - runs once before the test
 */
export function setup() {
    console.log('='.repeat(60));
    console.log('Mockery Load Test - GET /api/mocks (List Directory)');
    console.log('='.repeat(60));
    console.log(`Target URL: ${config.baseUrl}`);
    console.log(`List Path: ${listPath || '(root)'}`);
    console.log(`Target RPS: ${config.rps}`);
    console.log(`Duration: ${config.duration}`);
    console.log(`Scenario: ${scenarioType}`);
    console.log('='.repeat(60));

    // Verify the mocks endpoint is accessible
    const headers = listPath ? mockHeaders(listPath) : defaultHeaders;
    const warmupResponse = http.get(
        `${config.baseUrl}/api/mocks`,
        { headers }
    );

    if (warmupResponse.status !== 200) {
        console.warn(`Warning: Warmup request returned status ${warmupResponse.status}`);
        console.warn(`Response: ${warmupResponse.body}`);
    } else {
        console.log('Warmup successful. Sample response:');
        try {
            const body = JSON.parse(warmupResponse.body);
            console.log(`  Folders: ${body.folders?.length || 0}`);
            console.log(`  Files: ${body.files?.length || 0}`);
        } catch (e) {
            console.log(`  Raw: ${warmupResponse.body.substring(0, 200)}...`);
        }
    }

    return { config, listPath };
}

/**
 * Main test function - executed by each VU iteration
 */
export default function (data) {
    const url = `${data.config.baseUrl}/api/mocks`;
    const headers = data.listPath ? mockHeaders(data.listPath) : defaultHeaders;

    const startTime = Date.now();
    const response = http.get(url, { headers });
    const latency = Date.now() - startTime;

    // Record custom metrics
    listRequests.add(1);
    listLatency.add(latency);

    // Check response
    const success = check(response, {
        'status is 200': (r) => r.status === 200,
        'response is JSON': (r) => {
            try {
                JSON.parse(r.body);
                return true;
            } catch (e) {
                return false;
            }
        },
        'response has folders array': (r) => {
            try {
                const body = JSON.parse(r.body);
                return Array.isArray(body.folders);
            } catch (e) {
                return false;
            }
        },
        'response has files array': (r) => {
            try {
                const body = JSON.parse(r.body);
                return Array.isArray(body.files);
            } catch (e) {
                return false;
            }
        },
        'response time < 500ms': (r) => r.timings.duration < 500,
    });

    // Track success rate
    listSuccessRate.add(response.status === 200);
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
        config: {
            ...config,
            listPath: listPath,
        },
        metrics: {
            http_reqs: data.metrics.http_reqs?.values?.count || 0,
            http_req_duration_avg: data.metrics.http_req_duration?.values?.avg || 0,
            http_req_duration_p95: data.metrics.http_req_duration?.values['p(95)'] || 0,
            http_req_failed_rate: data.metrics.http_req_failed?.values?.rate || 0,
            list_success_rate: data.metrics.list_success?.values?.rate || 0,
        },
        thresholds_passed: Object.keys(data.thresholds || {}).every(
            (key) => data.thresholds[key].ok
        ),
    };

    return {
        stdout: JSON.stringify(summary, null, 2) + '\n',
        'k6/results/mocks-list-summary.json': JSON.stringify(summary, null, 2),
    };
}
