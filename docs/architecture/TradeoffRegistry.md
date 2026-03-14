# Architectural Tradeoff Registry

## Decision 1: Marten vs. EventStoreDB
- **Decision**: Use Marten (PostgreSQL-based Event Store).
- **Pros**:
    - Leverages existing PostgreSQL expertise within the institution.
    - Transactional consistency between events and the outbox (both in the same DB).
    - Excellent JSON indexing and querying support via Npgsql.
- **Cons**:
    - Not a native event store (some performance overhead vs. EventStoreDB).
- **Risk**: PostgreSQL storage growth for massive streams.
- **Mitigation**: Implement snapshots and aggressive event archival policies.

## Decision 2: Transactional Outbox vs. Two-Phase Commit (2PC)
- **Decision**: Use Transactional Outbox pattern.
- **Pros**:
    - High availability (no dependency on a distributed coordinator).
    - Strong consistency (event and outbox record updated in one transaction).
- **Cons**:
    - Latency between DB write and broker publication.
- **Risk**: Stale messages if the Outbox Processor fails.
- **Mitigation**: Real-time monitoring of outbox depth and automated alerts.

## Decision 3: Marten vs. Kafka (as primary store)
- **Decision**: Use Marten for the primary store, Service Bus for distribution.
- **Pros**:
    - Kafka is an event streaming platform, not an event store.
    - Marten provides ACID transactions, which are critical for financial settlement.
- **Cons**:
    - Higher write throughput potential in Kafka.
- **Risk**: Marten becoming a bottleneck.
- **Mitigation**: Horizontal scaling of PostgreSQL and read replicas.

## Decision 4: Why PostgreSQL?
- **Decision**: Standardized on PostgreSQL.
- **Pros**:
    - Open-source, no vendor lock-in.
    - Mature ecosystem, high stability.
    - Strong compliance with Tier-1 banking standards (pgAudit, SSL/TLS).
- **Cons**:
    - Not a distributed-first database (like CockroachDB).
- **Risk**: Availability of a single primary node.
- **Mitigation**: Use managed PostgreSQL services with multi-AZ failover.

## Decision 5: Why not Full CQRS Split?
- **Decision**: Simplified CQRS (Events + Outbox) for now, no separate read DB.
- **Pros**:
    - Lower operational complexity.
    - No eventual consistency issues within the primary application boundary.
- **Cons**:
    - Complex read queries can impact write performance.
- **Risk**: Analytical queries slowing down settlements.
- **Mitigation**: Offload heavy reads to PostgreSQL read-only replicas.
