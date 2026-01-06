# Mockery Load Tests (k6)

This directory contains k6 load test scripts for the Mockery API service.

## Prerequisites

Install k6 on your system:

```bash
# macOS
brew install k6

# Linux (Debian/Ubuntu)
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6

# Windows
winget install k6 --source winget

# Docker
docker pull grafana/k6
```

## Test Scripts

| Script | Description |
|--------|-------------|
| `scripts/mock-load.js` | Load test for GET `/api/mock` endpoint |
| `scripts/mocks-list.js` | Load test for GET `/api/mocks` (list directory) endpoint |
| `scripts/full-suite.js` | Combined workload with configurable traffic patterns |

## Configuration

All scripts support these environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `BASE_URL` | Target Mockery API URL | `http://localhost:8080` |
| `RPS` | Target requests per second | `10` |
| `DURATION` | Test duration (e.g., "30s", "5m", "1h") | `1m` |
| `MOCK_ID` | Mock ID to request | `FooBar/1234` |
| `VUS` | Virtual users (auto-calculated if not set) | Auto |
| `SCENARIO` | Scenario type: "constant" or "ramping" | `constant` |

## Quick Start

### Basic Test

```bash
# Start Mockery locally first
cd /path/to/Mockery
dotnet run --project src/Mockery

# Run load test with defaults (10 RPS for 1 minute)
k6 run k6/scripts/mock-load.js
```

### Custom RPS and Duration

```bash
# 50 requests per second for 5 minutes
k6 run -e RPS=50 -e DURATION=5m k6/scripts/mock-load.js

# 100 requests per second for 30 seconds
k6 run -e RPS=100 -e DURATION=30s k6/scripts/mock-load.js
```

### Target Different Environment

```bash
# Test against staging
k6 run -e BASE_URL=https://mockery-staging.example.com -e RPS=25 k6/scripts/mock-load.js

# Test against production (be careful!)
k6 run -e BASE_URL=https://mockery.example.com -e RPS=10 -e DURATION=30s k6/scripts/mock-load.js
```

### Test Different Mock

```bash
# Test specific mock
k6 run -e MOCK_ID=Products/hydrate -e RPS=20 k6/scripts/mock-load.js
```

## Test Scenarios

### Constant Arrival Rate (Default)

Maintains a steady request rate regardless of response time. Best for:
- Baseline performance testing
- Measuring consistent throughput
- Validating SLOs

```bash
k6 run -e RPS=50 -e DURATION=5m k6/scripts/mock-load.js
```

### Ramping Arrival Rate

Gradually increases load to find breaking points. Best for:
- Stress testing
- Finding capacity limits
- Identifying degradation patterns

```bash
k6 run -e SCENARIO=ramping -e RPS=100 -e DURATION=10m k6/scripts/mock-load.js
```

## Full Suite Testing

The `full-suite.js` script combines multiple endpoints with configurable traffic patterns.

### Test Modes

| Mode | GET Mock | LIST Mocks | Description |
|------|----------|------------|-------------|
| `mixed` | 70% | 30% | Balanced workload |
| `read-heavy` | 95% | 5% | Simulates production read patterns |
| `stress` | 80% | 20% | Ramps to 3x target RPS |

### Examples

```bash
# Mixed workload (default)
k6 run -e RPS=50 -e DURATION=5m k6/scripts/full-suite.js

# Read-heavy pattern
k6 run -e TEST_MODE=read-heavy -e RPS=100 -e DURATION=5m k6/scripts/full-suite.js

# Stress test (ramps to 3x RPS)
k6 run -e TEST_MODE=stress -e RPS=50 -e DURATION=10m k6/scripts/full-suite.js
```

## List Directory Testing

Test the `/api/mocks` endpoint for listing mock files.

```bash
# List root directory
k6 run -e RPS=20 k6/scripts/mocks-list.js

# List specific path
k6 run -e LIST_PATH=FooBar -e RPS=20 k6/scripts/mocks-list.js
```

## Performance Thresholds

Default thresholds (tests fail if exceeded):

| Metric | Standard | Stress |
|--------|----------|--------|
| p(50) latency | < 200ms | < 500ms |
| p(90) latency | < 500ms | < 2000ms |
| p(95) latency | < 1000ms | < 3000ms |
| p(99) latency | < 2000ms | < 5000ms |
| Error rate | < 1% | < 5% |
| Checks pass | > 99% | > 95% |

## Output and Results

### Console Output

k6 displays real-time metrics during test execution.

### JSON Summary

After each test, a JSON summary is saved to `k6/results/`:

```bash
# View latest results
cat k6/results/mock-load-summary.json | jq .
```

### Custom Output

```bash
# Output to JSON file
k6 run -e RPS=50 --out json=results.json k6/scripts/mock-load.js

# Output to InfluxDB
k6 run -e RPS=50 --out influxdb=http://localhost:8086/k6 k6/scripts/mock-load.js

# Output to Prometheus
k6 run -e RPS=50 --out experimental-prometheus-rw k6/scripts/mock-load.js
```

## Docker Usage

```bash
# Run with Docker
docker run -i --rm \
  -v $(pwd)/k6:/k6 \
  --network host \
  grafana/k6 run \
  -e BASE_URL=http://localhost:5000 \
  -e RPS=50 \
  -e DURATION=2m \
  /k6/scripts/mock-load.js
```

## Example Test Matrix

```bash
#!/bin/bash
# Run a series of load tests with increasing RPS

BASE_URL="http://localhost:5000"
DURATION="2m"

for RPS in 10 25 50 100 200; do
  echo "Testing at $RPS RPS..."
  k6 run \
    -e BASE_URL=$BASE_URL \
    -e RPS=$RPS \
    -e DURATION=$DURATION \
    k6/scripts/mock-load.js
  
  echo "Cooling down for 30 seconds..."
  sleep 30
done
```

## Interpreting Results

### Key Metrics

- **http_reqs**: Total number of HTTP requests made
- **http_req_duration**: Time spent making HTTP requests
- **http_req_failed**: Rate of failed requests
- **iterations**: Total number of test iterations completed
- **vus**: Number of active virtual users

### Custom Metrics

- **mock_served_success**: Rate of successful mock retrievals
- **mock_latency**: Trend of mock request latencies
- **list_success**: Rate of successful directory listings
- **total_requests**: Counter of all requests made

### Threshold Results

```
✓ http_req_duration..............: avg=45.23ms  min=12.34ms  med=42.11ms  max=234.56ms  p(90)=78.90ms  p(95)=98.76ms
✓ http_req_failed................: 0.00%  ✓ 0  ✗ 5000
✓ mock_served_success............: 100.00%  ✓ 5000  ✗ 0
```

- ✓ indicates threshold passed
- ✗ indicates threshold failed

## Troubleshooting

### Connection Refused

```
WARN[0000] Request Failed error="Get \"http://localhost:5000/api/mock\": dial tcp 127.0.0.1:5000: connection refused"
```

**Solution**: Ensure Mockery is running on the target URL.

### Too Many Open Files

```
WARN[0030] Request Failed error="socket: too many open files"
```

**Solution**: Increase file descriptor limits:
```bash
ulimit -n 65536
```

### High Error Rate

If you see high error rates, check:
1. Target server capacity
2. Network connectivity
3. Mock file existence
4. Server logs for errors

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Run Load Tests
  uses: grafana/k6-action@v0.3.0
  with:
    filename: k6/scripts/mock-load.js
    flags: -e BASE_URL=${{ secrets.STAGING_URL }} -e RPS=25 -e DURATION=2m
```

### Azure DevOps Example

```yaml
- script: |
    k6 run \
      -e BASE_URL=$(STAGING_URL) \
      -e RPS=25 \
      -e DURATION=2m \
      k6/scripts/mock-load.js
  displayName: 'Run k6 Load Tests'
