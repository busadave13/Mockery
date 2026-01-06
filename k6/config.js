// Shared configuration for k6 load tests
// This module provides common settings, thresholds, and utilities

/**
 * Get configuration from environment variables with defaults
 * @returns {Object} Configuration object
 */
export function getConfig() {
    return {
        baseUrl: __ENV.BASE_URL || 'http://localhost:8080',
        rps: parseInt(__ENV.RPS) || 10,
        duration: __ENV.DURATION || '1m',
        mockId: __ENV.MOCK_ID || 'FooBar/1234',
        vus: __ENV.VUS ? parseInt(__ENV.VUS) : null, // Auto-calculate if not specified
    };
}

/**
 * Calculate virtual users needed to achieve target RPS
 * Assumes average response time of 100ms (10 requests per VU per second)
 * @param {number} targetRps - Target requests per second
 * @param {number} expectedLatencyMs - Expected average latency in milliseconds
 * @returns {number} Number of virtual users needed
 */
export function calculateVUs(targetRps, expectedLatencyMs = 100) {
    // VUs = RPS * (latency in seconds)
    // Add 20% buffer for variability
    const vus = Math.ceil(targetRps * (expectedLatencyMs / 1000) * 1.2);
    return Math.max(vus, 1); // At least 1 VU
}

/**
 * Standard thresholds for load tests
 */
export const thresholds = {
    // HTTP request duration thresholds
    http_req_duration: [
        'p(50)<200',   // 50% of requests should be below 200ms
        'p(90)<500',   // 90% of requests should be below 500ms
        'p(95)<1000',  // 95% of requests should be below 1000ms
        'p(99)<2000',  // 99% of requests should be below 2000ms
    ],
    // HTTP request failure rate
    http_req_failed: ['rate<0.01'], // Less than 1% failure rate
    // Checks pass rate
    checks: ['rate>0.99'], // 99% of checks should pass
};

/**
 * Relaxed thresholds for stress testing
 */
export const stressThresholds = {
    http_req_duration: [
        'p(50)<500',
        'p(90)<2000',
        'p(95)<3000',
        'p(99)<5000',
    ],
    http_req_failed: ['rate<0.05'], // Allow up to 5% failure during stress
    checks: ['rate>0.95'],
};

/**
 * Create constant arrival rate scenario
 * @param {number} rate - Requests per second
 * @param {string} duration - Duration string (e.g., "1m", "5m")
 * @param {number} preAllocatedVUs - Pre-allocated VUs (optional)
 * @returns {Object} Scenario configuration
 */
export function constantArrivalRateScenario(rate, duration, preAllocatedVUs = null) {
    const vus = preAllocatedVUs || calculateVUs(rate);
    return {
        executor: 'constant-arrival-rate',
        rate: rate,
        timeUnit: '1s',
        duration: duration,
        preAllocatedVUs: vus,
        maxVUs: vus * 2, // Allow scaling up to 2x if needed
    };
}

/**
 * Create ramping arrival rate scenario
 * @param {number} targetRate - Target requests per second
 * @param {string} duration - Total duration string
 * @returns {Object} Scenario configuration
 */
export function rampingArrivalRateScenario(targetRate, duration) {
    const vus = calculateVUs(targetRate);
    // Parse duration to get total seconds
    const durationMatch = duration.match(/^(\d+)(s|m|h)$/);
    let totalSeconds = 60; // default 1 minute
    if (durationMatch) {
        const value = parseInt(durationMatch[1]);
        switch (durationMatch[2]) {
            case 's': totalSeconds = value; break;
            case 'm': totalSeconds = value * 60; break;
            case 'h': totalSeconds = value * 3600; break;
        }
    }
    
    // Ramp up over 20%, steady for 60%, ramp down over 20%
    const rampUpDuration = Math.ceil(totalSeconds * 0.2);
    const steadyDuration = Math.ceil(totalSeconds * 0.6);
    const rampDownDuration = totalSeconds - rampUpDuration - steadyDuration;
    
    return {
        executor: 'ramping-arrival-rate',
        startRate: 0,
        timeUnit: '1s',
        preAllocatedVUs: vus,
        maxVUs: vus * 2,
        stages: [
            { duration: `${rampUpDuration}s`, target: targetRate },
            { duration: `${steadyDuration}s`, target: targetRate },
            { duration: `${rampDownDuration}s`, target: 0 },
        ],
    };
}

/**
 * Default headers for Mockery API
 */
export const defaultHeaders = {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
};

/**
 * Create headers with X-Mockery-Mock
 * @param {string} mockId - Mock ID to request
 * @returns {Object} Headers object
 */
export function mockHeaders(mockId) {
    return {
        ...defaultHeaders,
        'X-Mockery-Mock': mockId,
    };
}
