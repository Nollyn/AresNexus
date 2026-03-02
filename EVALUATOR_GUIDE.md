# EVALUATOR_GUIDE: AresNexus Technical Audit

Welcome to the AresNexus Technical Evaluation. This guide is designed to provide you with a "Zero-Config" experience for auditing the system's resilience, consistency, and compliance architectures.

**Note for Evaluators:** This project was built using an AI-augmented workflow. The author provided the architectural blueprints and strategic direction, while the AI agent assisted in code generation and documentation formatting. This approach was chosen to simulate a high-velocity, modern engineering environment.

## 1. Quick Start (The "One-Command" Experience)
AresNexus is fully containerized and orchestrated via Docker and a root `Makefile`.

1. **Clone the repository.**
2. **Execute the following command in the root directory:**
   ```bash
   make up
   ```
   *This will pull/build the .NET 10 Chiseled images, start the Postgres (Marten) Event Store, RabbitMQ Broker, and the Observability stack (Prometheus/Grafana).*

3. **Verify Health:**
   - Gateway API: `http://localhost:5000/health`
   - Settlement Core: `http://localhost:5001/health`

## 2. Visual Audit (Swagger & Logging)
Once the stack is up, you can interact with the system visually:

- **Swagger UI (Settlement Core):** [http://localhost:5001/swagger](http://localhost:5001/swagger)
  - Explore the ISO 20022 transaction endpoints.
  - Review the XML documentation for domain logic.
- **Structured Logging:**
  - Run `docker compose logs -f settlement-core` to see the JSON-structured log stream required for Swiss Tier-1 auditability.

## 3. Data Integrity & Resilience Audit
To see the system in action and verify its resilience patterns:

1. **Automated Seeding:**
   On first startup, the `DataSeeder` automatically injects 5 historical transactions and 1 snapshot. You can see these immediately by fetching the account state in Swagger.

2. **Live Transaction Demo:**
   Execute the following to simulate a burst of ISO 20022 payments:
   ```bash
   make demo
   ```

3. **Consistency Check (The Outbox):**
   Access the **RabbitMQ Management UI** at [http://localhost:15672](http://localhost:15672) (Guest/Guest).
   - Observe the `outbox.messages` queue as transactions are processed.
   - Verify that events are persisted atomically with the aggregate state.

## 4. Architectural Pattern Audit Trail (Code-to-Concept)
To verify the implementation of core architectural patterns, please refer to the following key files:

- **Event Sourcing (Aggregate Root)**: `apps/settlement-core/src/AresNexus.Settlement.Domain/Aggregates/Account.cs`
  - *Audit Note*: Observe the `ApplyChange` and `Apply` methods for state mutation via events.
- **Transactional Outbox**: `apps/settlement-core/src/AresNexus.Settlement.Infrastructure/Messaging/OutboxProcessor.cs` and `MartenAccountRepository.cs`
  - *Audit Note*: The repository saves events and outbox messages in a single Marten transaction; the processor relays them.
- **Strict Idempotency**: `apps/settlement-core/src/AresNexus.Settlement.Infrastructure/Idempotency/IdempotencyMiddleware.cs`
  - *Audit Note*: Verification of the `Idempotency-Key` header against Redis.
- **Field-Level Encryption**: `apps/settlement-core/src/AresNexus.Settlement.Infrastructure/Security/PiiEncryptionService.cs`
  - *Audit Note*: AES-256 encryption applied to the `Reference` field in the command handler.
- **Automated Snapshotting**: `apps/settlement-core/src/AresNexus.Settlement.Infrastructure/Repositories/MartenAccountRepository.cs` (Line 123)
  - *Audit Note*: Threshold-based snapshotting to maintain O(1) recovery time.

## 5. Observability & "Golden Signals" (DORA Compliance)
The monitoring stack (Grafana/Prometheus) is pre-configured to track the four "Golden Signals" required for DORA operational resilience:

1. **Latency**: Tracked via `settlement_processing_seconds` histogram in `ProcessTransactionCommandHandler.cs`.
2. **Traffic**: Monitored through `settlement_total_count` (success/failure) counter.
3. **Errors**: Captured by status tags on metrics and structured Serilog logs.
4. **Saturation**: System-level metrics (CPU/RAM/Connection Pools) provided by .NET 10 runtime instrumentation.

**Grafana Dashboard**: [http://localhost:3000](http://localhost:3000) (Admin/Admin)
- Navigate to the **"Settlement Core - Executive Overview"** dashboard.
- This view maps directly to the FINMA requirement for real-time operational transparency.

## 6. Regulatory Mapping (DORA/FINMA)
The following files describe how AresNexus meets the 2026 DORA requirements:
- `/docs/14-operational-resilience-dora.md`: Detailed mapping of patterns to regulatory requirements.
- `README.md`: High-level overview of the resilience strategy.

---
**Audit Note:** All containers run as non-root users using .NET 10 Chiseled images to minimize the attack surface, in accordance with Swiss banking security standards.
