# Mockery Observability Stack

This directory contains the configuration for monitoring Mockery with Prometheus and Grafana.

## Architecture

```
┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│                 │     │                 │     │                 │
│    Mockery      │────▶│   Prometheus    │────▶│    Grafana      │
│  (metrics at    │     │  (scrapes /     │     │  (dashboards)   │
│   /metrics)     │     │   metrics)      │     │                 │
└─────────────────┘     └─────────────────┘     └─────────────────┘
```

## Quick Start

### Option 1: Local Development (Recommended for Development)

Run Mockery locally with `dotnet run`, and only run the observability stack in Docker:

```bash
# Terminal 1: Start Mockery locally
cd src/Mockery
dotnet run

# Terminal 2: Start observability stack (Prometheus + Grafana)
docker compose -f docker-compose.observability.yml up -d
```

### Option 2: Full Docker Stack

Run everything in Docker:

```bash
docker compose -f docker-compose.observability.yml -f docker-compose.observability.docker.yml up -d
```

## Accessing the Stack

| Service | URL | Credentials |
|---------|-----|-------------|
| Grafana | http://localhost:3000 | admin / admin |
| Prometheus | http://localhost:9090 | - |
| Mockery | http://localhost:8080 | - |
| Mockery Metrics | http://localhost:8080/metrics | - |

## Dashboards

### Mockery Throttling Dashboard

Pre-configured dashboard showing:

- **Throttling Configuration**: Status, rate limit, burst size, available tokens
- **Request Rates**: Allowed vs throttled requests over time
- **Token Bucket State**: Real-time visualization of the token bucket algorithm
- **Throttling Analysis**: Throttle rate percentage, request distribution
- **Mock Details**: Requests by mock ID and status code

Access: Grafana → Dashboards → Mockery → Mockery Throttling

## Metrics Exposed

Mockery exposes the following metrics at `/metrics`:

### Counters
| Metric | Description | Labels |
|--------|-------------|--------|
| `mockery_mocks_served_total` | Total mocks served | `mock_id`, `http_status_code` |
| `mockery_requests_throttled_total` | Requests rejected by rate limiting | - |
| `mockery_requests_total` | Total requests received | - |

### Gauges
| Metric | Description |
|--------|-------------|
| `mockery_throttling_enabled` | Whether throttling is enabled (1/0) |
| `mockery_throttling_rate_limit` | Configured requests per second |
| `mockery_throttling_burst_size` | Configured burst size |
| `mockery_throttling_tokens_available` | Current tokens in bucket |

## Load Testing with k6

Generate load to see metrics in action:

```bash
# Install k6 (macOS)
brew install k6

# Run load test with default settings (10 RPS, 1 minute)
k6 run k6/scripts/mock-load.js

# Customize RPS and duration
k6 run k6/scripts/mock-load.js --env RPS=50 --env DURATION=5m

# Ramping load test
k6 run k6/scripts/mock-load.js --env SCENARIO=ramping --env RPS=100 --env DURATION=3m
```

## Troubleshooting

### No Metrics Data in Grafana

1. **Check Prometheus targets**: http://localhost:9090/targets
   - Mockery target should show as "UP"
   - If "DOWN", check if Mockery is running and accessible

2. **Verify metrics endpoint**: `curl http://localhost:8080/metrics`
   - Should return Prometheus-formatted metrics

3. **Check correct compose file**:
   - Local dev: Use `docker-compose.observability.yml` (scrapes `host.docker.internal`)
   - Full Docker: Add `-f docker-compose.observability.docker.yml` (scrapes `mockery:8080`)

### Prometheus Can't Reach Mockery

**Local Development Issue**: Docker containers can't reach `localhost` on your host machine.

Solution: The `docker-compose.observability.yml` uses `prometheus-local.yml` which scrapes `host.docker.internal:8080`.

### Restart Prometheus After Config Changes

```bash
# Restart to pick up config changes
docker compose -f docker-compose.observability.yml restart prometheus

# Or use Prometheus reload endpoint
curl -X POST http://localhost:9090/-/reload
```

## Files Structure

```
observability/
├── README.md                    # This file
├── grafana/
│   ├── dashboards/
│   │   └── mockery-throttling.json    # Pre-configured dashboard
│   └── provisioning/
│       ├── dashboards/
│       │   └── dashboards.yml         # Dashboard provisioning
│       └── datasources/
│           └── datasources.yml        # Prometheus datasource
└── prometheus/
    ├── prometheus.yml           # Docker config (scrapes mockery:8080)
    └── prometheus-local.yml     # Local dev config (scrapes host.docker.internal)
```

## Cleanup

Stop and remove containers:

```bash
# Stop services
docker compose -f docker-compose.observability.yml down

# Remove volumes (deletes metrics history)
docker compose -f docker-compose.observability.yml down -v
