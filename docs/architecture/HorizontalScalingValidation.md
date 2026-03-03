# Horizontal Scaling Validation

This document details the validation of AresNexus's horizontal scaling capabilities, ensuring that the system remains consistent, idempotent, and highly available when running multiple instances.

## 1. Stateless API Design
The `Settlement.Api` is designed to be completely stateless. 
- **No Shared Memory:** No in-memory caching that requires synchronization between instances. 
- **Distributed State:** All operational state (Rate limiting, idempotency keys) is stored in **Redis**.
- **Database as Truth:** Marten (PostgreSQL) handles all aggregate persistence and event sourcing.

## 2. Concurrency Control
Horizontal scaling introduces the risk of race conditions. AresNexus mitigates this using:
- **Optimistic Concurrency (OCC):** Every event stream in Marten is versioned. If two instances attempt to append events to the same aggregate simultaneously, only one will succeed; the other will receive a `ConcurrencyException` and can retry based on policy.
- **Idempotency Keys:** Every command can be submitted with an `X-Idempotency-Key`. Redis ensures that even if a load balancer retries a request to a different instance, the logic is executed only once.

## 3. Transactional Outbox & Safe Dispatch
A critical challenge in horizontal scaling is ensuring the Outbox doesn't dispatch duplicate messages.
- **PostgreSQL Advisory Locks:** The `OutboxProcessor` uses `pg_advisory_xact_lock(12345)` to ensure that only one instance processes the outbox batch at a time.
- **Scaling Workers:** While currently locked to one worker for strict ordering, the design allows for partitioned locking if higher throughput is required.

## 4. Failure Scenarios
| Scenario | Behavior |
| --- | --- |
| **Instance Crash** | Load balancer (Nginx) detects failure via `/health` check and redirects traffic to healthy instances. |
| **Network Partition** | Redis/Postgres serve as the quorum. Instances unable to reach the DB will fail health checks. |
| **Duplicate Request** | Redis-backed idempotency filter catches duplicates across instances. |

## 5. Simulation Setup
The `docker-compose.yaml` is configured with:
- 3 API instances (`settlement-core-1`, `settlement-core-2`, `settlement-core-3`).
- Nginx Load Balancer for round-robin distribution.
- Shared PostgreSQL and Redis.

## 6. Validation Results
Integration tests (`ScalingTests.cs`) simulate 100+ concurrent requests hitting multiple instances, validating that:
- [x] Zero duplicate events are persisted.
- [x] Optimistic concurrency correctly rejects conflicting updates.
- [x] Outbox remains stable and ordered.
