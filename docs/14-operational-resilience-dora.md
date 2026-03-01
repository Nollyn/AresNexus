# Operational Resilience & DORA Compliance

AresNexus is designed to meet the rigorous requirements of the **Digital Operational Resilience Act (DORA)**, coming into full effect in 2026. This document details the architectural patterns and implementations that ensure continuous monitoring, high availability, and effective risk management for Swiss Tier-1 banking.

## 1. Zero-Lag Observability (OpenTelemetry)

To comply with DORA's requirements for continuous monitoring and rapid incident detection, AresNexus implements comprehensive distributed tracing and metrics:

-   **End-to-End Traceability**: Every command entering the `GatewayAPI` or `SettlementCore` is assigned a unique `TraceId` and `CorrelationId`.
-   **Propagation**: These identifiers are propagated across asynchronous boundaries using the **Transactional Outbox** and **Azure Service Bus** application properties.
-   **OpenTelemetry Integration**: Standardized instrumentation using `OpenTelemetry.Instrumentation.AspNetCore` and `OpenTelemetry.Exporter.OpenTelemetryProtocol` (OTLP) for seamless integration with modern observability stacks (Grafana, Jaeger, etc.).

## 2. Long-Term Evolution (Event Upcasting)

DORA emphasizes the longevity and auditability of financial records. As the system evolves, historical events must remain readable:

-   **IEventUpcaster**: A robust interface and base class mechanism that allows on-the-fly transformation of legacy JSON schemas into modern domain objects during event replay.
-   **Schema Evolution**: This ensures that breaking changes in domain logic do not invalidate the historical audit trail of the account.

## 3. Resilience through Chaos Engineering

To prove operational resilience, we employ chaos testing:

-   **Network Partition Testing**: Scripts in `/scripts/chaos` simulate database and message bus disconnects.
-   **Outbox Relay Recovery**: These tests verify that the `BackgroundService` correctly pauses, retries with exponential backoff, and automatically resumes processing once connectivity is restored, ensuring **at-least-once** delivery guarantees.

## 4. Swiss Security Hardening

-   **API Rate Limiting**: Implemented via .NET 10 middleware. High-risk endpoints (like transactions) use a restrictive policy to prevent DDoS attacks and ensure fair resource distribution.
-   **Infrastructure Redundancy**: Kubernetes `PodDisruptionBudget` ensures that even during cluster upgrades or maintenance, a minimum of 2 replicas of the `SettlementCore` remain operational, maintaining the 99.99% availability target.

## 5. Third-Party Risk Management

AresNexus reduces third-party risk by:

-   **Idempotency**: Using Redis-backed idempotency to handle potential duplicate retries from third-party payment gateways or flaky network components.
-   **Mocking & Fallbacks**: Providing mock implementations for cloud services (like Azure Key Vault) for local development and testing, ensuring the core logic is decoupled from specific cloud provider outages.

---
*Last Updated: 2026-03-01*
*Compliance Officer: AresNexus Architect Team*
