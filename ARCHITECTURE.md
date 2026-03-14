# Strategic Architecture Decision Records (ADRs)

This document outlines the high-level architectural decisions and trade-offs made for the AresNexus settlement system. These decisions were directed by the Principal Architect to ensure compliance with Swiss Tier-1 (FINMA/DORA) resilience and auditability standards.

## ADR-001: Event Sourcing for Settlement Core

### Context
Traditional CRUD systems only store the "Current State." In a Tier-1 financial settlement system, knowing the current balance is insufficient; we must know *exactly* how we arrived at that state, with an immutable audit trail for every transaction.

### Decision
Implement **Event Sourcing** using Marten (PostgreSQL) for the Account aggregate. Every financial movement is captured as an immutable event (e.g., `FundsDeposited`, `FundsWithdrawn`).

### Rationale & Trade-offs
- **Pros**:
  - **Immutable Audit Trail**: Guaranteed by design. We never "Update" or "Delete" history.
  - **Temporal Queries**: Ability to reconstruct the account state at any point in time.
  - **Fine-Grained Observability**: Events provide more context than a simple state change.
- **Cons**:
  - **Complexity**: Higher cognitive load for developers and complex state recovery.
  - **Versioning**: Requires `EventUpcasters` to handle schema evolution over time.
  - **Performance**: Reading long event streams can be slow (mitigated by **Automated Snapshotting**).

## ADR-002: Transactional Outbox Pattern

### Context
In distributed systems, we often need to update a database *and* publish a message to a broker (RabbitMQ). This is the "Dual-Write" problem. If the database update succeeds but the broker fails, the system becomes inconsistent.

### Decision
Implement the **Transactional Outbox Pattern**. Instead of sending messages directly to the broker, we save them to an `OutboxMessages` table in the same ACID transaction as the domain events.

### Rationale & Trade-offs
- **Pros**:
  - **Atomic Consistency**: The system state and its notifications are always in sync.
  - **At-Least-Once Delivery**: A background relay process ensures every message is eventually delivered to the broker.
  - **Resilience**: The system can continue processing transactions even if the message broker is temporarily down.
- **Cons**:
  - **Infrastructure Complexity**: Requires a dedicated table and a background relay service (`OutboxProcessor`).
  - **Potential Duplicates**: Consumers must be idempotent (addressed by **Strict Idempotency** middleware).

## ADR-003: Strict Idempotency & Command Validation

### Context
Financial instructions (e.g., "Pay 1,000 CHF") are often sent multiple times due to network retries or user error. Duplicate processing is a catastrophic failure in a settlement system.

### Decision
Enforce **Strict Idempotency** via a mandatory `Idempotency-Key` (UUID) header for all transaction-modifying requests. Verification is performed using a high-performance Redis store.

### Rationale & Trade-offs
- **Pros**:
  - **Financial Safety**: Zero tolerance for duplicate transactions.
  - **DORA Compliance**: Directly addresses operational resilience requirements.
- **Cons**:
  - **External Dependency**: Adds Redis to the critical path (mitigated by high-availability Redis clustering).
  - **Protocol Overhead**: Clients must manage and send unique keys.

## ADR-004: Chiseled Containers & Zero-Trust Security

### Context
The attack surface of standard container images (e.g., Alpine or Debian) is too large for Tier-1 banking.

### Decision
Standardize on **.NET 10 Chiseled Images** and enforce **Kubernetes Network Policies** for microsegmentation.

### Rationale & Trade-offs
- **Pros**:
  - **Minimal Attack Surface**: No shell, no package manager, no root user in the container.
  - **Microsegmentation**: Services can only talk to explicitly authorized dependencies.
- **Cons**:
  - **Debugging Difficulty**: Requires specialized tools (e.g., `kubectl debug`) as traditional tools are absent from the image.
