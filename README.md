# Ares-Nexus

[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Clean_Architecture_%7C_DDD-darkgreen)](https://en.wikipedia.org/wiki/Domain-driven_design)
[![Pattern](https://img.shields.io/badge/Pattern-Event_Sourcing-darkred)](https://martinfowler.com/eaaDev/EventSourcing.html)
[![Orchestration](https://img.shields.io/badge/Orchestration-Kubernetes-blue)](https://kubernetes.io/)
[![Compliance](https://img.shields.io/badge/Compliance-FINMA%20%2F%20DORA-red)](https://www.finma.ch/en/)

Welcome to the Ares-Nexus solution. This project is a robust, event-driven settlement system built with .NET 10.

## Overview

Ares-Nexus is designed to handle complex settlement processes with a focus on scalability, resilience, and domain-driven design (DDD). It utilizes event sourcing to maintain a complete history of all account-related activities.

## Architecture Choice

This project utilizes Minimal APIs (.NET 10) to minimize vertical overhead and leverage the latest performance optimizations of the Kestrel server, moving away from the traditional Mvc/Controller pattern for a leaner, high-throughput execution.

## Core Pillars (Strategic Highlights)

- **Event Sourcing (Marten)**: Complete, immutable audit trail of all financial movements, ensuring 100% auditability for FINMA compliance.
- **Transactional Outbox**: Atomic consistency between domain state changes and external notifications, solving the "Dual-Write" problem.
- **Strict Idempotency**: Redis-backed command validation ensuring every financial instruction is processed exactly once.
- **Performance Snapshotting**: Automated state capturing every 100 events to maintain sub-millisecond recovery times as streams grow.
- **Field-Level Encryption**: AES-256 hardening of PII data within events before persistence, meeting Tier-1 banking privacy standards.

## Tech Stack Justification

- **.NET 10 (Chiseled)**: Chosen for its industry-leading performance, native AOT capabilities, and reduced attack surface (via Chiseled containers), essential for high-frequency settlement.
- **PostgreSQL + Marten**: Leverages the stability of Postgres with the power of Marten as a Document Store and Event Store, providing ACID compliance and flexible schema evolution.
- **RabbitMQ**: Selected for its robust message queuing and support for complex routing patterns, ensuring resilient inter-service communication.
- **Redis**: Provides high-performance distributed locking and idempotency checks required for DORA operational resilience.

## AI Governance & Leadership

While this repository utilized **Junie** (JetBrains AI Agent) for rapid scaffolding, boilerplate generation, and mechanics, all **Architectural Thinking, System Design Decisions, and Pattern Selection** (including the implementation of the Transactional Outbox, Event Sourcing strategy, and FINMA/DORA compliance framework) were directed, reviewed, and validated by the author. This project demonstrates a modern **'Architect-as-Orchestrator'** workflow—leveraging AI for mechanical execution while maintaining absolute human-led strategic integrity.

## Project Structure

- **apps/settlement-core**: The primary settlement system.
  - **Api**: ASP.NET Core Web API providing the transaction interface.
  - **Application**: Layer containing MediatR commands, handlers, and validators.
  - **Domain**: Core business logic, aggregate roots (Account), and domain events.
  - **Infrastructure**: Event Store (Marten/PostgreSQL) and Messaging (RabbitMQ/Redis) adapters.
- **apps/compliance-engine**: A secondary service (Python-based) for transaction compliance checks.
- **shared**: Common libraries.
  - **AresNexus.Shared.Kernel**: Common DDD primitives, base `AggregateRoot`, and event interfaces.
- **infrastructure**: Deployment and configuration assets.
  - **kubernetes**: K8s manifests for zero-trust networking and resilient deployments.
- **monitoring**: Observability stack configurations.
  - **prometheus**: Alerting rules and scraping config.
  - **grafana**: Pre-configured dashboards for settlement monitoring.
- **docs**: Comprehensive documentation and Architectural Decision Records (ADRs).

## Documentation

For a deeper dive into the architecture and design decisions, please refer to the [docs](/docs) folder.

### Strategic & Architecture
- [Architecture Vision & ADRs](ARCHITECTURE.md) - Project goals and high-level roadmap.
- [Evaluator Audit Guide](EVALUATOR_GUIDE.md) - Quick-start for auditing the system's resilience.
- [Architecture Details](/docs/03-architecture-definition.md) - Low-level technical specifications.
- [Visual Architecture (C4)](/docs/04-visual-architecture-c4.md) - C4 Model diagrams.

### Design Decisions
- [ADR 001: Event Sourcing](/docs/02-adr-001-event-sourcing.md) - Why we chose Event Sourcing for settlements.

### Operations & Infrastructure
- [Implementation Plan](/docs/06-implementation-plan.md) - Phase-by-phase execution strategy.
- [Resilience and Scalability](/docs/05-resilience-and-scalability.md) - High availability and disaster recovery patterns.
- [Infrastructure as Code](/docs/07-infrastructure-as-code-manifest.md) - Overview of IaC approach.
- [Portfolio Summary](/docs/10-portfolio-summary.md) - Executive overview of the solution.

## Getting Started

### Prerequisites
- .NET 10 SDK
- Docker (for containerized execution)
- Kubernetes (local or remote cluster for deployment)

### Running the API (Local Development)
1. Navigate to the API project directory:
   ```powershell
   cd apps/settlement-core/src/AresNexus.Settlement.Api
   ```
2. Run the application:
   ```powershell
   dotnet run
   ```
3. Access the Scalar API documentation at `http://localhost:5136/scalar/v1`.

## Containerization & Orchestration

The solution is cloud-native and ready for Kubernetes deployment.

### Kubernetes Manifests
Located in `infrastructure/kubernetes/`:
- **Resilience Manifests**: (`08-k8s-resilience-manifest.yaml`, `09-k8s-resilience-manifest.yaml`) Define deployments with anti-affinity rules, resource limits, and health probes (Liveness/Readiness) to ensure zero-downtime rolling updates.
- **Network Policies**: (`08-k8s-network-policy.yaml`, `08b-k8s-network-policy.yaml`) Implement a Zero-Trust security model, restricting ingress/egress traffic to only authorized services (e.g., Gateway to API, API to Event Store).

### Monitoring Stack
- **Prometheus**: Scrapes metrics from `/metrics` endpoints using OpenTelemetry.
- **Grafana**: Provides visual dashboards (see `monitoring/grafana/dashboard-settlement.json`).

## Features

- **Event Sourcing**: Complete audit trail of all account transactions.
- **Zero-Trust Security**: Kubernetes Network Policies for microsegmentation.
- **High Availability**: Multi-replica deployments with pod anti-affinity.
- **Observability**: Fully integrated with OpenTelemetry, Prometheus, and Grafana.
- **Modern API**: Built with Minimal APIs, Versioning, and Scalar for documentation.

## Seniority Upgrades (Audit-Ready & Resilient)

To meet Swiss Banking Resilience (FINMA & DORA compliance) standards, the following pillars have been implemented:

1.  **Atomic Consistency (Transactional Outbox)**: Atomic persistence of domain events and integration messages within the same database transaction. A dedicated BackgroundService (The Relay) ensures at-least-once delivery to Azure Service Bus, fulfilling FINMA requirements for reliable cross-service communication.
2.  **Financial Safety (Strict Idempotency)**: Mandatory `IdempotencyKey` (UUID) validation for all transaction commands using a Redis-backed middleware. This prevents duplicate processing of financial instructions, a critical requirement for DORA operational resilience.
3.  **Performance (Snapshotting & Upcasting)**: Automated aggregate snapshotting every 100 events and a robust `EventUpcaster` base class for schema evolution. This ensures sub-millisecond state recovery and long-term data maintainability.
4.  **Security (Field-Level Encryption)**: AES-256 encryption for sensitive fields (`Reference` and `Metadata`) in financial events before they are persisted to the database. This provides defense-in-depth and meets Tier-1 banking standards for data privacy.
5.  **Operational Resilience (Kubernetes Hardening)**: Implementation of `ResourceQuota` to limit CPU/RAM per namespace and `PodDisruptionBudget` to ensure 99.99% availability during cluster maintenance and upgrades.

## Quick Start for Evaluators

1.  **Clone** the repository.
2.  **`make up`**: Pulls/Builds everything and starts the infrastructure stack (Postgres, RabbitMQ, Redis, Prometheus, Grafana).
3.  **`make demo`**: Sends a burst of ISO 20022 transactions to populate the system and verify the end-to-end flow.
4.  **Open `http://localhost:5001/swagger`**: Explore the Settlement Core API.
5.  **`make test`**: Runs all Unit, Integration, and Architecture tests.

## Swiss Tier-1 Compliance

AresNexus is engineered to meet the stringent standards set by **FINMA** (Swiss Financial Market Supervisory Authority) and **DORA** (Digital Operational Resilience Act):

- **Traceability**: Every financial movement is captured as an immutable event.
- **Integrity**: Transactional Outbox ensures that the system state and its external notifications are always in sync.
- **Availability**: Kubernetes hardening and graceful degradation patterns ensure the system remains operational under stress.
- **Privacy**: Field-level encryption ensures that PII (Personally Identifiable Information) is never stored in plain text.

## License

This project is licensed under the MIT License.

## AI Governance & Leadership

While this repository utilized **Junie** (JetBrains AI Agent) for rapid scaffolding, boilerplate generation, and mechanics, all **Architectural Thinking, System Design Decisions, and Pattern Selection** (including the implementation of the Transactional Outbox, Event Sourcing strategy, and FINMA/DORA compliance framework) were directed, reviewed, and validated by the author. This project demonstrates a modern **'Architect-as-Orchestrator'** workflow—leveraging AI for mechanical execution while maintaining absolute human-led strategic integrity.
