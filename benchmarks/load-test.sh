#!/usr/bin/env bash
# Ares-Nexus Load Test: "Market Peak" burst using k6
# Prerequisites: k6 installed (https://k6.io/) and stack running (make up)
# Usage examples:
#   ./benchmarks/load-test.sh smoke      # quick 30s
#   ./benchmarks/load-test.sh stress     # 5m burst + 5m sustain
#   ./benchmarks/load-test.sh soak       # 30m steady-state

set -euo pipefail

MODE=${1:-smoke}
BASE_URL=${BASE_URL:-http://localhost:5001}
VUS_SMOKE=${VUS_SMOKE:-100}
VUS_STRESS=${VUS_STRESS:-1000}
VUS_SOAK=${VUS_SOAK:-500}

K6_SCRIPT=$(cat <<'K6'
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = __ENV.K6_OPTIONS ? JSON.parse(__ENV.K6_OPTIONS) : {
  vus: 50,
  duration: '30s',
  thresholds: {
    http_req_duration: ['p(99)<50'], // p99 < 50ms target
    http_req_failed: ['rate<0.001'], // <0.1% errors
  },
};

const BASE = __ENV.BASE_URL || 'http://localhost:5001';

export default function () {
  // Create a synthetic transaction payload (ISO-like)
  const payload = JSON.stringify({
    accountId: '11111111-2222-3333-4444-555555555555',
    amount: { currency: 'CHF', value: Math.floor(Math.random() * 1000) + 1 },
    reference: `BF-${Math.random().toString(36).substring(2, 10)}`,
    idempotencyKey: `${__VU}-${Date.now()}-${Math.random()}`
  });

  const res = http.post(`${BASE}/api/settlements`, payload, {
    headers: { 'Content-Type': 'application/json', 'Idempotency-Key': `${__ITER}-${__VU}-${Date.now()}` },
    tags: { endpoint: 'create_settlement' }
  });

  check(res, {
    'status is 200/202': (r) => r.status === 200 || r.status === 202,
  });

  // R/W mix: read back state (CQRS projection)
  const r = http.get(`${BASE}/api/accounts/11111111-2222-3333-4444-555555555555`);
  check(r, { 'read ok': (rr) => rr.status === 200 });

  sleep(Math.random() * 0.2); // small jitter
}
K6
)

case "$MODE" in
  smoke)
    K6_OPTIONS=$(jq -nc --arg vus "$VUS_SMOKE" '{vus: ($vus|tonumber), duration:"30s"}') ;;
  stress)
    # ramp up -> burst -> sustain -> ramp down
    K6_OPTIONS='{"stages":[{"duration":"2m","target":200},{"duration":"3m","target":500},{"duration":"5m","target":500},{"duration":"2m","target":0}],"thresholds":{"http_req_duration":["p(99)<50"],"http_req_failed":["rate<0.001"]}}' ;;
  soak)
    K6_OPTIONS=$(jq -nc --arg vus "$VUS_SOAK" '{vus: ($vus|tonumber), duration:"30m"}') ;;
  *)
    echo "Unknown mode: $MODE" >&2; exit 1 ;;
endcase

export BASE_URL
export K6_OPTIONS

echo "Running k6 with mode=$MODE BASE_URL=$BASE_URL"
# shellcheck disable=SC2086
k6 run - < <(printf "%s" "$K6_SCRIPT")
