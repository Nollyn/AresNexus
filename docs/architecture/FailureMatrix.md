# Failure Matrix

| Failure Type | Detection | Recovery | Data Loss Risk | SLA Impact |
| :--- | :--- | :--- | :--- | :--- |
| **Transient Database Failure** | Polly `RetryPolicy` | Automatic retry with backoff | Zero | Latency spike (P95+) |
| **Sustained Database Failure** | Polly `CircuitBreakerPolicy` | Manual / Auto after 30s cooling period | Zero | Partial unavailability |
| **Broker Outage (Azure SB)** | `OutboxProcessor` exception | Continuous background retry | Zero (Outbox) | Event delivery delay |
| **Poison Outbox Message** | `AttemptCount >= 5` | DLQ flagging (`IsPoison=true`) | Zero (Preserved in DB) | Individual message lag |
| **In-Memory Buffer Full** | Backpressure / `SoftThrottling` | Request throttling / rejection | Zero | Throughput dip |
| **API Replica Crash** | Kubernetes Liveness / Readiness | Auto-restart by K8s | Zero (Stateless) | Negligible (Redundancy) |
| **Network Partition (Split Brain)** | Marten Optimistic Concurrency | `RetryBoundary` / Conflict resolution | Zero | Latency spike |

## Legend
- **Detection**: How the system identifies the failure mode.
- **Recovery**: How the system returns to a healthy state.
- **Data Loss Risk**: Assessment of whether data could be lost during this failure.
- **SLA Impact**: Expected effect on service level agreements.
