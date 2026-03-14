# Service Level Objectives (SLOs) & SLAs

## 1. Overview
AresNexus operates as a Tier-1 financial system. Our targets are mathematically grounded in our **Benchmark Results** and constrained by our **Architecture Limits** (Marten, PostgreSQL, Outbox).

## 2. Core Numerical Targets

| Metric | SLO Target | SLA Commitment | Measurement Basis |
| :--- | :--- | :--- | :--- |
| **Availability** | **99.99%** | 99.95% | Successful HTTP 2xx/3xx/4xx (excl. 429) |
| **P95 Latency** | **< 25ms** | < 40ms | Request -> Response (Gateway) |
| **P99 Latency** | **< 50ms** | < 100ms | Request -> Response (Gateway) |
| **Throughput** | **10,000 TPS** | 8,000 TPS | Sustained k6 Load Test |
| **MTTR (Resilience)** | **< 30s** | < 60s | Automated Failover to Secondary |
| **RPO** | **0** | 0 | Event Sourcing (ACID Persistence) |
| **RTO** | **< 30s** | < 60s | Outbox catch-up and failover |

## 3. Error Budget & Downtime
Based on a **30-day rolling window**:

- **99.99% (SLO)**: 4.38 minutes per month allowable downtime.
- **99.95% (SLA Breach)**: 21.92 minutes per month allowable downtime.

### Error Budget Calculation
`Error Budget = (1 - SLO) * Total_Requests`
*Example: At 1M requests/day, we allow 100 failed requests per day before the budget is exhausted.*

## 4. Architectural Tie-ins

### Latency vs. Snapshots
- Our P99 SLO (< 50ms) is tied to the `SnapshotInterval=100`. Aggregates with > 5,000 events without snapshots would exceed this target during replay.
- **Enforcement**: CI/CD includes a "Replay Benchmark" that fails if any aggregate type exceeds 100ms reconstruction time.

### Throughput vs. Rate Limiting
- The API is rate-limited at **100 req/10s** per client. This protects our **Database I/O Bottleneck** identified in benchmarks, ensuring a single client cannot starve the event store connection pool.

### Availability vs. Circuit Breakers
- Database circuit breakers (`ResiliencePolicyFactory.cs`) open after **5 consecutive failures** with a **30s break**. This prevents cascading failures but counts against our Availability SLO.

## 5. Breach Handling & Escalation Path

### Level 1: SLO Warning (80% Budget Burn)
- **Trigger**: Error budget burn rate > 2x over 1 hour.
- **Action**: Automated Slack alert to SRE and Product teams. Feature flag freeze.

### Level 2: SLO Violation
- **Trigger**: Error budget exhausted.
- **Action**: P1 incident opened. Review of "Incident Postmortem" process. Post-mortem review with CTO.

### Level 3: SLA Breach
- **Trigger**: Availability < 99.95% or P99 > 100ms over 30 days.
- **Action**: Formal report to Swiss Architecture Governance Board. Root Cause Analysis (RCA) delivered to Risk & Compliance within 24 hours.

## 6. Reporting
Metrics are collected via Prometheus and visualized in the **Executive SLO Dashboard**. Reports are generated weekly for ARB review.
