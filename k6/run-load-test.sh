#!/bin/bash
# =============================================================================
# K6 Load Test Runner for Mockery
# =============================================================================
# A wrapper script to run load tests against the Mockery API with configurable
# parameters for RPS, duration, base URL, and mock ID.
# =============================================================================

set -e

# Default values (matching load-test.js defaults)
RPS=10
DURATION="30s"
BASE_URL="http://mockery.local.com"
MOCK_ID="mockery/success"
K6_EXTRA_ARGS=()

# Get the directory where this script is located
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# =============================================================================
# Help function
# =============================================================================
show_help() {
    cat << EOF
Mockery K6 Load Test Runner

Usage: $(basename "$0") [OPTIONS] [-- K6_OPTIONS]

Options:
    --rps <number>        Requests per second (default: $RPS)
    --duration <duration> Test duration, e.g., 30s, 1m, 5m (default: $DURATION)
    --base-url <url>      Base URL of the Mockery service (default: $BASE_URL)
    --mock-id <id>        Mock ID to request via X-Mockery-Mock header (default: $MOCK_ID)
    --help, -h            Show this help message

K6 Passthrough:
    Any arguments after '--' are passed directly to k6.
    Example: $(basename "$0") --rps 100 -- --out json=results.json

Examples:
    # Run with defaults (10 RPS for 30 seconds)
    $(basename "$0")

    # Run at 100 RPS for 1 minute against localhost
    $(basename "$0") --rps 100 --duration 1m --base-url http://localhost:8080

    # Run with a specific mock ID
    $(basename "$0") --mock-id FooBar/1234

    # Run and export results to JSON
    $(basename "$0") --rps 50 -- --out json=results.json

    # Run with summary export
    $(basename "$0") -- --summary-export=summary.json

EOF
}

# =============================================================================
# Check if k6 is installed
# =============================================================================
check_k6_installed() {
    if ! command -v k6 &> /dev/null; then
        echo "Error: k6 is not installed or not in PATH."
        echo ""
        echo "Installation instructions:"
        echo "  macOS:   brew install k6"
        echo "  Linux:   sudo apt-get install k6"
        echo "  Windows: choco install k6"
        echo "  Docker:  docker run --rm -i grafana/k6 run - <script.js"
        echo ""
        echo "For more options, visit: https://grafana.com/docs/k6/latest/set-up/install-k6/"
        exit 1
    fi
}

# =============================================================================
# Parse command line arguments
# =============================================================================
parse_args() {
    while [[ $# -gt 0 ]]; do
        case $1 in
            --rps)
                RPS="$2"
                shift 2
                ;;
            --duration)
                DURATION="$2"
                shift 2
                ;;
            --base-url)
                BASE_URL="$2"
                shift 2
                ;;
            --mock-id)
                MOCK_ID="$2"
                shift 2
                ;;
            --help|-h)
                show_help
                exit 0
                ;;
            --)
                shift
                K6_EXTRA_ARGS=("$@")
                break
                ;;
            *)
                echo "Unknown option: $1"
                echo "Use --help for usage information."
                exit 1
                ;;
        esac
    done
}

# =============================================================================
# Run the load test
# =============================================================================
run_load_test() {
    echo "=============================================="
    echo "Mockery K6 Load Test"
    echo "=============================================="
    echo "Configuration:"
    echo "  RPS:      $RPS"
    echo "  Duration: $DURATION"
    echo "  Base URL: $BASE_URL"
    echo "  Mock ID:  $MOCK_ID"
    if [[ ${#K6_EXTRA_ARGS[@]} -gt 0 ]]; then
        echo "  K6 Args:  ${K6_EXTRA_ARGS[*]}"
    fi
    echo "=============================================="
    echo ""

    k6 run \
        -e "RPS=$RPS" \
        -e "DURATION=$DURATION" \
        -e "BASE_URL=$BASE_URL" \
        -e "MOCK_ID=$MOCK_ID" \
        "${K6_EXTRA_ARGS[@]}" \
        "$SCRIPT_DIR/load-test.js"
}

# =============================================================================
# Main
# =============================================================================
main() {
    parse_args "$@"
    check_k6_installed
    run_load_test
}

main "$@"
