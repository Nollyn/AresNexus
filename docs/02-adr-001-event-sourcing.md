# ADR 001: Event Sourcing for Financial Integrity

## Status
Proposed / Accepted

## Context
In Swiss Tier-1 Banking, "State-based" (CRUD) systems are insufficient for high-stakes audits. If a balance is incorrect, a CRUD system cannot prove *how* it reached that state without relying on unreliable application logs.

## Decision
We will implement **Event Sourcing** as the primary persistence pattern for the Settlement Core. Every financial transaction will be stored as an immutable sequence of events.

## Business Rationale
- **100% Audit Traceability**: Meets FINMA's most stringent requirements for "proven consistency," removing the need for expensive, error-prone reconciliation sub-systems.
- **Systemic Risk Mitigation**: Eliminates "Dual-Write" fragility. The event *is* the state, ensuring the ledger and downstream reports are never out of sync.
- **Operational Resilience**: Enables "Time-Travel" debugging and rapid state reconstruction, significantly reducing MTTR during complex production incidents.

## Consequences
*   **Pros:**
    *   **Auditability:** Built-in, 100% accurate audit trail (FINMA requirement).
    *   **Time-Travel:** Ability to reconstruct the state of any account at any point in time.
    *   **Scalability:** High-performance writes (append-only) decoupled from complex read queries via CQRS.
*   **Cons:**
    *   **Complexity:** Higher learning curve for the engineering team.
    *   **Versioning:** Requires a strict strategy for "Event Upcasting" (schema evolution).

## Risk Mitigation
To mitigate "Talent Risk" and complexity, we will use a standardized Event Store (Azure CosmosDB with Change Feed) and provide an Internal Developer Platform (IDP) to abstract the event-bus logic.
