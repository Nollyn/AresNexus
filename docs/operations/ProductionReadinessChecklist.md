# AresNexus Production Readiness Checklist

This document defines the formal requirements for a service to be considered production-ready in the AresNexus Swiss Tier-1 environment.

## 1. Architecture
- [x] **Bounded Context**: Clear boundaries defined between Settlement, Compliance, and Gateway services.
- [x] **Concurrency Strategy**: Optimistic concurrency implemented via Marten versioning (`Account.Version`).
- [x] **Event Versioning**: Schema evolution strategy defined (Upcasters implemented in `Upcasters.cs`).
- [x] **Snapshot Strategy**: Validated every 100 events to bound replay time < 100ms.

## 2. Resilience
- [x] **Circuit Breakers**: Configured in `ResiliencePolicyFactory.cs` for all database and external calls.
- [x] **Retry Limits**: Exponential backoff implemented with a maximum of 3 retries for transient DB errors.
- [x] **Dead-Letter Handling**: Poison message detection implemented in `OutboxProcessor.cs` (max 5 attempts before marking as `IsPoison`).
- [x] **Backpressure**: Rate limiting enforced at API level via `PartitionedRateLimiter` (Fixed window: 100 requests / 10s).

## 3. Observability
- [x] **SLOs Defined**: Documented in `docs/operations/SLOs.md`.
- [x] **Error Budget**: Monthly downtime budget calculated (4.38 minutes for 99.99%).
- [x] **Alert Thresholds**: P95 latency > 200ms and Error Rate > 0.1% configured in Prometheus.
- [x] **Correlation Propagation**: TraceId and CorrelationId passed from API to Event Store and Outbox (verified in `MartenAccountRepository.cs`).

## 4. Security
- [x] **Threat Model**: Reviewed for FINMA compliance (PII protection, audit trails).
- [x] **Rate Limiting**: Enforced for high-risk endpoints (`/withdraw`) and global API.
- [x] **Idempotency**: Handled via Event Sourcing (Natural duplicate detection by Aggregate ID + Version).
- [x] **Secrets Management**: No secrets in source code; Azure Key Vault or Environment Variables used for connection strings.
- [x] **PII Protection**: AES-256 encryption for sensitive fields in events before persistence.

## 5. Data
- [x] **Backup Strategy**: Daily full backups + WAL (Write-Ahead Log) archiving for Point-In-Time Recovery (PITR).
- [x] **Restore Validation**: Quarterly automated restore tests into staging environment.
- [x] **Retention Policy**: 10-year retention for financial events as per Swiss law; 24-hour retention for processed outbox messages.
- [x] **Event Archival**: Strategy defined for off-loading events > 2 years to cold storage while maintaining replayability.

## 6. Deployment
- [x] **Blue/Green Deployment**: Configured via Kubernetes Ingress/Service mesh for zero-downtime cutover.
- [x] **Rollback Plan**: Automated rollback if health checks fail or error rate spikes > 1% post-deployment.
- [x] **Schema Safety**: Marten configured to validate schemas on startup; manual migration scripts for complex refactors.
- [x] **Zero-Downtime**: Validated via `dotnet-counters` and load testing during service restarts.

---

### Verification Authority
| Stakeholder | Role | Approval |
| :--- | :--- | :--- |
| **Chief Architect** | Design & Scalability | [ ] |
| **CISO** | Security & Compliance | [ ] |
| **SRE Lead** | Reliability & Ops | [ ] |
| **Head of Risk** | Financial Integrity | [ ] |
