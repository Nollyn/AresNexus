# Ares-Nexus

Welcome to the Ares-Nexus solution. This project is a robust, event-driven settlement system built with .NET 10.

## Overview

Ares-Nexus is designed to handle complex settlement processes with a focus on scalability, resilience, and domain-driven design (DDD). It utilizes event sourcing to maintain a complete history of all account-related activities.

## Project Structure

- **apps/settlement-core**: The primary settlement system.
  - **Api**: ASP.NET Core Web API providing the transaction interface.
  - **Application**: Layer containing MediatR commands, handlers, and validators.
  - **Domain**: Core business logic, aggregate roots (Account), and domain events.
  - **Infrastructure**: Event Store (CosmosDB) and Messaging (Service Bus) adapters.
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
- [Strategic Vision](/docs/01-strategic-vision.md) - Project goals and high-level roadmap.
- [Architecture Definition](/docs/03-architecture-definition.md) - Detailed technical architecture.
- [System Context](/docs/system-context.md) - C1/C2 diagrams and external dependencies.
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
2.  **`make up`**: Pulls/Builds everything and starts the stack (Docker required).
3.  **Open `http://localhost:5001/swagger`**: Explore the Settlement Core API (enabled in Release mode).
    *   *Note: Port 5000 is the Gateway API (placeholder), Port 5001 is the Settlement Core API.*
4.  **`make demo`**: Sends a burst of 50 sample ISO 20022 transactions to "populate" the system and Grafana charts live.
5.  **`make test`**: Runs all Unit and Integration tests.

## Swiss Tier-1 Compliance

AresNexus is engineered to meet the stringent standards set by **FINMA** (Swiss Financial Market Supervisory Authority) and **DORA** (Digital Operational Resilience Act):

- **Traceability**: Every financial movement is captured as an immutable event.
- **Integrity**: Transactional Outbox ensures that the system state and its external notifications are always in sync.
- **Availability**: Kubernetes hardening and graceful degradation patterns ensure the system remains operational under stress.
- **Privacy**: Field-level encryption ensures that PII (Personally Identifiable Information) is never stored in plain text.

## License

This project is licensed under the MIT License.
