# Technical Architecture & Resilience Framework

## 1. Core Patterns
*   **CQRS (Command Query Responsibility Segregation):** Separates the high-stakes write model (Event Store) from the high-speed read model (PostgreSQL/Redis), ensuring **0ms-proximate latency** for dashboarding and queries.
*   **Saga Pattern (Orchestration):** Manages distributed transactions across "Settlement" and "Compliance" contexts. If Compliance rejects a payment, a **Compensating Transaction** is triggered to revert reserved funds.
*   **Outbox Pattern:** Ensures "At-Least-Once" delivery of events to the message broker, preventing data loss during network partitions.

## 2. Resilience Strategy (The 99.99% Target)
*   **Cellular Architecture:** Deploying "Cells" of the application across multiple Azure Availability Zones.
*   **Self-Healing:** Kubernetes (AKS) liveness and readiness probes combined with Horizontal Pod Autoscaling (HPA).
*   **Circuit Breakers:** Implementation of [Resilience4Net/Polly] to prevent cascading failures when external Swiss Interbank Clearing (SIC) systems are slow.

## 3. Security (Zero Trust)
*   **Identity:** Every microservice has a Managed Identity. No static passwords/secrets in code.
*   **mTLS:** Encrypted communication between all pods via Linkerd/Istio Service Mesh.
*   **Policy-as-Code:** OPA (Open Policy Agent) gates to ensure all deployments meet Swiss security baseline (ISO 27001).
