# Incident Postmortem (Simulated): Outbox Processor Lag

**Incident ID**: SIM-2026-001  
**Severity**: SEV-1 (Critical Business Impact)  
**Status**: Resolved  
**Date**: 2026-03-03  
**Service**: Settlement Core / Outbox Processor

## 1. Executive Summary
During a high-load period (quarter-end settlement burst), the Transactional Outbox processor experienced significant lag. This resulted in a delay of up to 45 minutes for settlement confirmations reaching the downstream Clearing & Custody systems, although the internal event store remained consistent and ACID-compliant.

## 2. Timeline (UTC)
- **09:00**: Start of peak settlement window. Volume increases 10x from baseline (200 TPS -> 2,000 TPS).
- **09:15**: `Outbox_Lag_Seconds` alert triggers (Warning: > 60s).
- **09:20**: `Outbox_Lag_Seconds` alert triggers (Critical: > 300s).
- **09:25**: On-call engineer acknowledges; starts investigation.
- **09:35**: Identified bottleneck in Outbox Processor polling loop due to table bloat and lack of `SKIP LOCKED`.
- **09:45**: Temporary scaling: Increased Outbox Processor replicas from 1 to 4. Contention increases.
- **10:05**: Applied hotfix: Introduced Advisory Locks and `SKIP LOCKED` query pattern.
- **10:45**: Outbox lag returns to < 1s.
- **11:00**: Incident closed.

## 3. Detection & Blast Radius
- **Detection**: Prometheus alert on `aresnexus_outbox_lag_seconds`.
- **Blast Radius**: Downstream systems (Clearing, Reporting) received settlement events with high latency. Client-facing dashboards showed "Pending" status for completed transactions.
- **Customer Impact**: 1,200 institutional clients experienced delayed settlement finality notifications. No data loss occurred.

## 4. Root Cause Analysis (5 Whys)
1. **The settlement confirmations were delayed.**
   - Because the Outbox Processor could not keep up with the ingestion rate of the Event Store.
2. **The Outbox Processor could not keep up.**
   - Because the polling query `SELECT ... WHERE ProcessedOnUtc IS NULL` became slow as the table grew.
3. **The polling query became slow.**
   - Because of PostgreSQL MVCC bloat; the index on `ProcessedOnUtc` contained too many "dead" entries that hadn't been vacuumed yet.
4. **The processor could not be scaled horizontally effectively.**
   - Because multiple instances were competing for the same rows, leading to transaction rollbacks and lock wait timeouts.
5. **The system lacked a partitioned or notify-based dispatch mechanism.**
   - We relied on a single-worker polling model which is a known architectural bottleneck for Tier-1 throughput levels.

## 5. Immediate Mitigation
- Scaled database CPU to handle increased lock contention.
- Manually triggered `VACUUM ANALYZE` on the `OutboxMessages` table.
- Deployed a configuration change to increase batch size from 50 to 500.

## 6. Long-Term Corrective Actions
- [x] **Advisory Locks**: Implement PostgreSQL advisory locks to allow safe horizontal scaling of workers (Implemented in `OutboxProcessor.cs`).
- [ ] **Partitioning**: Partition `OutboxMessages` by `OccurredOnUtc` (Daily) to keep the active set small.
- [ ] **Notify/Listen**: Move from polling to `NOTIFY/LISTEN` for sub-millisecond dispatch.
- [ ] **Archival**: Implement an aggressive archival strategy for processed outbox messages (> 24h).

## 7. Architectural Lessons Learned
- **Polling is not Scaling**: In high-throughput event sourcing, the outbox is often the first point of failure.
- **MVCC Awareness**: Database-backed queues must be tuned for aggressive vacuuming.
- **Observability Gap**: We lacked a "Processing Rate vs. Ingestion Rate" delta metric, which would have predicted the lag 30 minutes earlier.

## 8. Corrective Implementation Applied

### Outbox Batching and Parallel Dispatch
Following the analysis of the lag incident, we have refactored the `OutboxProcessor` to improve throughput without compromising strict ordering or safety.

- **Change**: Increased batch size from 50 to 100 messages per polling cycle.
- **Change**: Replaced sequential message publishing with `Task.WhenAll` parallel dispatch within each batch.
- **Why**: Sequential publishing was the primary bottleneck under high network latency to the Service Bus. Parallelizing the I/O-bound publish operation significantly reduces the total processing time per batch.
- **Expected Improvement**: Estimated 3-5x increase in outbox throughput, reducing lag accumulation during peak bursts.
- **Risk**: Increased concurrent connections to the message broker. Mitigated by the fixed batch size (100).

---

## 🔥 Phase 5 — Engineering Scar Tissue: What we would change if rebuilding today

While the current architecture is robust and meets all Swiss Tier-1 compliance requirements, 24 months of production "scar tissue" suggests the following shifts if we were starting from a blank slate today:

### 1. Moving to Partitioned Event Streams
Currently, we use a single Marten event store. At our projected growth (0.5 TB/year), the global event store will hit management limits within 3-5 years. We would adopt a **Partitioned Event Store** strategy (e.g., partitioning by Client ID or Value Date) at the database level to allow independent scaling and faster backups.

### 2. Async Projection Read Models by Default
We currently perform some synchronous work during the command path. To achieve < 50ms P99 consistently under any load, we would move **all** non-ACID requirements (like reporting views and secondary indexes) to purely asynchronous Marten projections.

### 3. Introducing Kafka as the "System of Record" for Integration
Relying on a DB-backed outbox for all downstream integration adds load to our primary OLTP database. If rebuilding, we would use a **Change Data Capture (CDC)** tool like Debezium to stream Marten events directly into Kafka, removing the `OutboxMessages` table entirely from the critical path.

### 4. Physical Command/Query Separation
Splitting the Command (Write) and Query (Read) databases physically would allow us to tune the Write DB for pure append-only performance and the Read DB for complex Swiss regulatory reporting without contention.

### 5. Deterministic Simulation Testing
Instead of just unit/integration tests, we would invest in **Deterministic Simulation Testing** (similar to FoundationDB) to catch rare race conditions in our resilience logic before they hit production.
