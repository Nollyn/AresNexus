# AresNexus Architecture Decision Whitepaper
## Executive Summary

**AresNexus** is a staff-level reference architecture designed specifically for **Tier-1 financial institutions** requiring the highest standards of auditability, resilience, and regulatory compliance. It provides a blueprint for a high-performance banking settlement core, demonstrating how to handle mission-critical financial transactions with absolute integrity.

### The Problem it Solves
Modern financial systems face a triple-challenge:
1.  **Regulatory Compliance (DORA, MiFID II, AML):** Traditional CRUD systems struggle to provide a complete, immutable history of state transitions, often relying on fragile audit tables.
2.  **Systemic Resilience:** Distributed systems must handle transient failures gracefully without data loss or corruption.
3.  **High Throughput & Low Latency:** Settlement cores must process thousands of transactions per second while maintaining strict consistency.

AresNexus solves these by employing an **Event Sourced** architecture backed by **Marten (PostgreSQL)**, ensuring every state change is recorded as an immutable event, enabling perfect audit trails and reliable state derivation.

---

## Why Event Sourcing Here?

### Justification
In a high-risk financial domain, the *process* of reaching a state is often as important as the state itself. Event sourcing is appropriate for AresNexus because:
- **Auditability & Traceability:** Every transaction is a series of events. We don't just know that an account balance is $1M; we know exactly which deposits and withdrawals led to that balance.
- **Regulatory Alignment:** Immobility is a core requirement for non-repudiation. Once an event is appended to the stream, it cannot be altered.
- **Replayability:** In the event of a bug or system failure, we can "rewind" the system state to any point in time and replay events to verify correctness.

### Tradeoffs
- **Complexity:** Higher cognitive load for developers compared to CRUD.
- **Storage:** Event stores grow monotonically.
- **Projection Maintenance:** Read models must be updated asynchronously, introducing eventual consistency in some views.

---

## Tradeoffs: Event Sourcing vs Traditional CRUD

| Feature | Event Sourcing (AresNexus) | Traditional CRUD |
| :--- | :--- | :--- |
| **Auditability** | Native, immutable, and complete. | Bolted-on via audit tables; often incomplete. |
| **State Mutation** | State is derived from a stream of events. | State is overwritten in place. |
| **Complexity** | High (Event schemas, projections, upcasters). | Low (Standard ORMs, simple tables). |
| **Operational Cost** | Higher (Requires event store management). | Lower (Standard DB management). |
| **Recovery** | Full state reconstruction from zero. | Point-in-time recovery via DB backups. |

---

## Why Marten? (PostgreSQL-backed Event Store)

AresNexus uses **Marten** as its event store and document database. 

### Rationale
- **Operational Maturity:** PostgreSQL is a Tier-1 database with world-class support for ACID transactions, replication, and disaster recovery.
- **Document Database Model:** The flexibility of JSONB in PostgreSQL allows for evolving event schemas without the overhead of rigid relational migrations.
- **Transactional Consistency:** Marten leverages PostgreSQL's native transactions to ensure that events and projections (or outbox entries) are saved atomically.

### Tradeoffs vs EventStoreDB
- **Pros:** No new infrastructure required if PostgreSQL is already in the stack; SQL-based reporting on event data.
- **Cons:** Not as horizontally scalable as a dedicated event store like EventStoreDB for extreme-scale event streams (billions of events).

---

## Why Transactional Outbox Instead of 2PC?

AresNexus implements the **Transactional Outbox** pattern for reliable messaging.

### The Constraint
In a microservices architecture, **Two-Phase Commit (2PC)** is impractical because:
1. It is a blocking protocol that reduces availability (violating CAP theorem "A" under partition).
2. It does not scale horizontally and creates tight coupling between services.

### The AresNexus Solution
The Outbox pattern ensures **Reliability**:
1. We save the business state (events) and the outgoing message (outbox) in a single local database transaction.
2. A background worker (Outbox Processor) polls the outbox and publishes messages to the broker.
3. This guarantees **At-Least-Once Delivery** without the overhead of distributed transactions.

---

## Risk Mitigation Strategy

- **Data Integrity:** Guaranteed by PostgreSQL ACID properties and Marten's optimistic concurrency.
- **Idempotency:** Every command includes a `CommandId`. The `IdempotencyGuard` checks if a command has already been processed before executing business logic.
- **Exactly-Once Semantics:** Achieved practically through "At-Least-Once Delivery" + "Idempotent Consumers".
- **Observability:** Full OpenTelemetry integration (Tracing & Metrics) to track transaction flow across services.
- **Governance:** CI/CD pipelines include automated architecture tests (NetArchTest) and security scanning (Trivy).

---

## Operational Model

- **Failure Scenarios:** Transient DB failures are handled by built-in retry policies (Standard Resilience Handler).
- **Recovery Scenarios:** System state can be rebuilt by replaying events from Marten into fresh projections.
- **Horizontal Scaling:** The API layer is stateless and scales horizontally. PostgreSQL can be scaled via read-replicas for projections or sharding for extreme loads.
- **Backpressure:** Managed via rate limiting at the API Gateway and message broker ingestion limits.

---
*Created by the Principal Architecture Team, 2026.*
