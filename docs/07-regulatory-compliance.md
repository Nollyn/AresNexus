# Regulatory & Risk Mapping: Swiss Financial Market Standards

This document details how the Ares-Nexus architecture specifically addresses the regulatory requirements set by **FINMA** and the **Digital Operational Resilience Act (DORA)**.

## FINMA 2023/1 (Operational Risk and Resilience)

Ares-Nexus implements the **Transactional Outbox Pattern** to ensure that every ledger update is eventually consistent with downstream reporting and external systems.

- **Regulatory Requirement**: Proven consistency and auditability of financial records.
- **Ares-Nexus Implementation**: All domain events (e.g., `AccountCreated`, `TransactionProcessed`) are persisted in the same database transaction as the aggregate state change. A dedicated `OutboxProcessor` ensures that these events are delivered to the message broker (RabbitMQ/Service Bus) at-least-once.
- **Business Value**: Eliminates the "Dual-Write" problem where a database update succeeds but a message notification fails, leading to systemic reconciliation gaps.

## DORA (Digital Operational Resilience Act - 2026)

Ares-Nexus is designed for **Resilience-by-Design**, meeting DORA's stringent requirements for ICT risk management and operational continuity.

- **Regulatory Requirement**: Systems must demonstrate the ability to withstand, respond to, and recover from all types of ICT-related disruptions.
- **Ares-Nexus Implementation**: 
    - **Chaos Engineering**: Automated scripts in `/infrastructure/chaos` simulate failures (e.g., killing pods, disconnecting brokers) to verify system recovery.
    - **Kubernetes Hardening**: `PodDisruptionBudgets` and anti-affinity rules ensure that the settlement engine remains available even during infrastructure maintenance or partial cluster failure.
    - **Verification**: The integration test `Resilience_ShouldRecoverFromBrokerFailure` proves that the system automatically recovers and "catches up" once infrastructure is restored.

## Swiss Banking Secrecy & Data Privacy (PII)

To comply with Swiss Banking Secrecy and GDPR, Ares-Nexus implements **Field-Level Encryption** for all Personally Identifiable Information (PII) within the event store.

- **Regulatory Requirement**: Protection of client-identifying data at rest and in transit.
- **Ares-Nexus Implementation**: Sensitive fields such as `Reference` and `Metadata` in financial events are encrypted using **AES-256** before being stored in the database.
- **Business Value**: Even in the event of a database compromise, client-sensitive data remains unintelligible without the encryption keys stored in a secure HSM or Key Vault.

## Strategic Guardrails: Dependency Isolation

To mitigate "Substitution Risk" and ensure long-term maintainability, Ares-Nexus enforces strict architectural boundaries.

- **Architecture Guardrail**: The `Domain` project has **zero external dependencies**.
- **Verification**: Enforced via `NetArchTest` in `tests/AresNexus.Tests.Architecture`, ensuring that core business logic remains independent of infrastructure, frameworks, and third-party libraries.
