# Threat Model - AresNexus Settlement Core

## Trust Boundaries
1.  **Public API Edge**: Boundary between the internet (via Gateway) and the Settlement API.
2.  **Internal Network**: Boundary between the Settlement API and the PostgreSQL database (Marten).
3.  **Broker Boundary**: Boundary between the Settlement API and Azure Service Bus.

## Attack Surface
-   **REST Endpoints**: CRUD operations on accounts and transactions.
-   **Event Store**: The persistence layer containing immutable transaction history.
-   **Outbox Processor**: Background worker responsible for message publishing.

## Identified Threats & Mitigations

### 1. Replay Attacks
-   **Threat**: An attacker intercepts a signed request and replays it to duplicate a deposit or withdrawal.
-   **Mitigation**: 
    -   **Idempotency Keys**: All state-changing commands require a unique `Idempotency-Key` (Requirement #3).
    -   **Sequence Validation**: Marten's optimistic concurrency (version checking) prevents the same event version from being applied twice.

### 2. Idempotency Abuse
-   **Threat**: An attacker floods the system with unique idempotency keys to exhaust storage or processing power.
-   **Mitigation**:
    -   **TTL on Idempotency**: Idempotency records are stored in Redis with a configurable TTL (e.g., 24 hours).
    -   **Rate Limiting**: Throttling requests per client before they reach the command handler.

### 3. Injection Risks
-   **Threat**: SQL Injection or NoSQL injection via malicious JSON payloads.
-   **Mitigation**:
    -   **Marten/Npgsql**: Marten uses parameterized queries by default.
    -   **Strong Typing**: Using C# records and DTOs ensures payload validation before processing.

### 4. Resource Exhaustion (DoS)
-   **Threat**: Flooding the API with requests to consume CPU, memory, or database connections.
-   **Mitigation**:
    -   **Rate Limiting**: Per-client and per-endpoint throttling.
    -   **Backpressure Strategy**: Soft throttling when the processing queue reaches critical length.
    -   **Circuit Breakers**: Prevent cascading failure to the database during high-load scenarios.

### 5. PII Data Leakage
-   **Threat**: Sensitive customer data (e.g., transaction references) is leaked via database dumps or logs.
-   **Mitigation**:
    -   **At-Rest Encryption**: `IEncryptionService` encrypts sensitive fields (Requirement #4) before persistence.
    -   **Sensitive Data Masking**: Serilog policies mask PII in logs.

### 6. Event Store Tampering
-   **Threat**: An attacker with DB access modifies the event store to alter history.
-   **Mitigation**:
    -   **Immutability**: Marten's event store is designed as an append-only log.
    -   **Audit Logging**: Database-level auditing (pgAudit) tracks all modifications.
