import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 200 }, // ramp up to 200 users
    { duration: '1m', target: 200 },  // stay at 200 users (simulating ~1000 TPS)
    { duration: '30s', target: 0 },   // ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<200', 'p(99)<500'], // 95% of requests must complete below 200ms
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  const payload = JSON.stringify({
    accountId: '550e8400-e29b-41d4-a716-446655440000',
    money: { amount: 10, currency: 'CHF' },
    transactionType: 'DEPOSIT',
    idempotencyKey: Math.random().toString(36).substring(7),
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
    },
  };

  const res = http.post(`${BASE_URL}/api/v1/settlement/transactions`, payload, params);

  check(res, {
    'status is 200 or 202': (r) => r.status === 200 || r.status === 202,
  });

  sleep(0.1); // Small sleep to maintain steady throughput
}
