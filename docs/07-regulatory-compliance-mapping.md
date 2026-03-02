# Regulatory Compliance Mapping

## Overview

Ares-Nexus is built to comply with Swiss Financial Market (FINMA) and Digital Operational Resilience (DORA) standards. This document details how specific technical architectural decisions address key regulatory scenarios.

## Technical Scenario Analysis

### Scenario A: Regulatory Audit Request (FINMA Compliance)

**The Requirement:** FINMA 2023/1 requires the ability to demonstrate a full audit trail and the state of any account at any point in the past.

**The Solution: Immutable Audit Trails (Event Sourcing)**
- **Marten-backed Event Store**: Every account transaction is captured as a permanent, immutable event in a PostgreSQL-based stream.
- **Full Reconstruction**: Account state is never overwritten; it is derived by replaying the stream. This provides 100% reconstruction for any historical timestamp.
- **Metadata Context**: Each event includes audit-ready metadata, including `CorrelationId`, `CausationId`, and the user/system that initiated the change.

### Scenario B: Infrastructure Failure (DORA Compliance)

**The Requirement:** DORA mandates that critical financial systems maintain operational resilience and prevent data loss (e.g., "Double Spending") even during major cloud regional outages.

**The Solution: Atomic Consistency (Transactional Outbox) & Idempotency**
- **Transactional Outbox**: By committing the domain event and the outbox message in a single database transaction, we ensure the system is never in a partial state. No message is sent if the state update fails.
- **Saga Pattern**: Multi-service settlements are managed as distributed transactions (Sagas) that can revert via compensating actions if a downstream failure occurs.
- **Strict Idempotency**: All commands require a unique `IdempotencyKey`. If a client retries a command during an Azure regional outage or recovery phase, the system identifies the duplicate and ensures it is processed exactly once.

### Scenario C: Swiss Client Privacy (Bank Secrecy)

**The Requirement:** Swiss banking laws and GDPR necessitate the protection of PII (Personally Identifiable Information), even from highly privileged internal actors like Database Administrators.

**The Solution: AES-256 Field-Level Encryption**
- **In-Memory Encryption**: Sensitive fields (e.g., `Reference`, `Metadata`) are encrypted in-memory *before* being serialized into the event stream.
- **Separated Key Management**: Encryption keys are stored in a secure Key Vault (e.g., Azure Key Vault, HashiCorp Vault), meaning a DBA with full database access cannot read client-sensitive data.
- **Selective Exposure**: Only authorized services with appropriate key access can decrypt and read PII fields, ensuring data secrecy by design.

## Compliance Traceability Matrix

| Regulation | Domain | Implementation | Verification |
|------------|--------|----------------|--------------|
| **FINMA 2023/1** | Operational Risk | Event Sourcing, Snapshots | Historical State Recovery Tests |
| **FINMA 2023/1** | Governance | Audit Metadata, Versioning | Architecture Tests (Metadata enforcement) |
| **DORA** | Resilience | Transactional Outbox, Chaos Testing | Chaos Mesh / MTTR Verification |
| **DORA** | Recovery | Point-in-Time Recovery (PITR) | Disaster Recovery Drills |
| **Swiss Bank Secrecy** | Privacy | Field-Level Encryption (AES-256) | Security Audit, Code Inspection |
