# Service Level Objectives (SLOs)

## Overview
As a Tier-1 financial institution, AresNexus must adhere to strict Service Level Agreements (SLAs). Our Service Level Objectives (SLOs) define the internal targets that help us achieve these SLAs.

## 1. Availability SLO
- **Target**: 99.99% (Four Nines)
- **Measurement**: Percentage of successful REST API requests (2xx, 3xx, 4xx excluding 429) over total requests.
- **Window**: 30-day rolling window.
- **Error Budget**: ~4.38 minutes of downtime per month.

## 2. Latency SLO
- **P95 Latency**: < 100ms for account creation and balance queries.
- **P99 Latency**: < 500ms for high-load transaction commands (deposit/withdraw).
- **Measurement**: Time from request reception at the API gateway to response dispatch.

## 3. Durability SLO
- **Target**: 99.999999999% (Eleven Nines)
- **Measurement**: Zero data loss for confirmed transactions.
- **Verification**: Nightly consistency checks between the Event Store and the Transactional Outbox.

## 4. Alert Triggers
| Metric | Threshold | Action |
| :--- | :--- | :--- |
| **Availability Drop** | < 99.95% (5m) | P1 Page to On-Call Architect |
| **P95 Latency Spike** | > 200ms (10m) | Auto-scale API replicas |
| **Error Budget Burn** | > 20% in 24h | Freeze non-critical feature deployments |
| **Outbox Lag** | > 10,000 messages | P2 Alert - Check Broker connectivity |

## 5. Reporting
SLO compliance reports are generated weekly and reviewed by the Architecture Review Board (ARB).
