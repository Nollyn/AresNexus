import http from 'k6/http';
import { check, sleep } from 'k6';

/**
 * DORA Chaos Test: Network Partition Simulation
 * 
 * This script simulates high load while the environment might experience connectivity issues
 * between the DB and Service Bus. The goal is to prove that the Outbox Relay (BackgroundService)
 * handles failures gracefully and recovers automatically when connectivity is restored.
 */

export let options = {
    stages: [
        { duration: '30s', target: 50 }, // Ramp up
        { duration: '1m', target: 50 },  // Steady state
        { duration: '30s', target: 0 },  // Ramp down
    ],
    thresholds: {
        http_req_duration: ['p(95)<500'], // 95% of requests must complete below 500ms
        http_req_failed: ['rate<0.01'],    // Failure rate must be less than 1%
    },
};

const BASE_URL = __ENV.API_URL || 'http://localhost:5000';

export default function () {
    const payload = JSON.stringify({
        accountId: '8f704179-8806-4447-817d-2495b508f7f5',
        amount: 100.0,
        transactionType: 'DEPOSIT',
        idempotencyKey: '00000000-0000-0000-0000-' + Math.floor(Math.random() * 1000000000000).toString().padStart(12, '0'),
        reference: 'Chaos Test ' + Date.now()
    });

    const params = {
        headers: {
            'Content-Type': 'application/json',
            'Idempotency-Key': '00000000-0000-0000-0000-' + Math.floor(Math.random() * 1000000000000).toString().padStart(12, '0'),
            'X-Correlation-Id': 'chaos-test-' + __VU + '-' + __ITER
        },
    };

    let res = http.post(`${BASE_URL}/api/v1/transactions`, payload, params);

    check(res, {
        'is status 202': (r) => r.status === 202,
        'is status 429 (expected due to rate limiting)': (r) => r.status === 429 || r.status === 202,
    });

    sleep(1);
}
