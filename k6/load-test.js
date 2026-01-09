import http from 'k6/http';
import { check } from 'k6';
import { Rate, Counter, Trend } from 'k6/metrics';

// =============================================================================
// Configuration from environment variables
// =============================================================================
const RPS = parseInt(__ENV.RPS) || 10;
const DURATION = __ENV.DURATION || '30s';
const BASE_URL = __ENV.BASE_URL || 'http://mockery.local.com';
const MOCK_ID = __ENV.MOCK_ID || 'mockery/success';

// Calculate VUs needed for target RPS (assuming ~100ms latency)
const VUS = Math.max(Math.ceil(RPS * 0.12), 1);

// =============================================================================
// Custom metrics
// =============================================================================
const successRate = new Rate('success_rate');
const requestCount = new Counter('requests_total');
const latency = new Trend('latency_ms', true);

// =============================================================================
// k6 options
// =============================================================================
export const options = {
    scenarios: {
        load_test: {
            executor: 'constant-arrival-rate',
            rate: RPS,
            timeUnit: '1s',
            duration: DURATION,
            preAllocatedVUs: VUS,
            maxVUs: VUS * 2,
        },
    },
    thresholds: {
        http_req_duration: ['p(95)<500'],  // 95% of requests under 500ms
        http_req_failed: ['rate<0.01'],    // Less than 1% failure rate
        success_rate: ['rate>0.99'],       // 99% success rate
    },
};

// =============================================================================
// Setup - runs once before the test
// =============================================================================
export function setup() {
    console.log('');
    console.log('='.repeat(60));
    console.log('  Mockery Load Test');
    console.log('='.repeat(60));
    console.log(`  RPS:      ${RPS} requests/second`);
    console.log(`  Duration: ${DURATION}`);
    console.log(`  Base URL: ${BASE_URL}`);
    console.log(`  Mock ID:  ${MOCK_ID}`);
    console.log(`  VUs:      ${VUS}`);
    console.log('='.repeat(60));
    console.log('');

    // Warmup request to verify endpoint is accessible
    const response = http.get(`${BASE_URL}/api/mock`, {
        headers: { 'X-Mockery-Mock': MOCK_ID }
    });

    if (response.status !== 200) {
        console.warn(`⚠ Warmup failed: status ${response.status}`);
    }

    return { baseUrl: BASE_URL, mockId: MOCK_ID };
}

// =============================================================================
// Main test function - executed by each VU iteration
// =============================================================================
export default function (data) {
    const start = Date.now();
    
    const response = http.get(`${data.baseUrl}/api/mock`, {
        headers: { 'X-Mockery-Mock': data.mockId }
    });

    const duration = Date.now() - start;

    // Record metrics
    requestCount.add(1);
    latency.add(duration);
    successRate.add(response.status === 200);

    // Checks
    check(response, {
        'status is 200': (r) => r.status === 200,
        'has content': (r) => r.body && r.body.length > 0,
        'response time < 500ms': (r) => r.timings.duration < 500,
    });
}

// =============================================================================
// Teardown - runs once after the test
// =============================================================================
export function teardown(data) {
    console.log('');
    console.log('='.repeat(60));
    console.log('  Load test completed');
    console.log('='.repeat(60));
    console.log('');
}