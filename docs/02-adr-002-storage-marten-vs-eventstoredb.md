# ADR 002: Marten (PostgreSQL) vs. EventStoreDB for Event Sourcing

## Status
Accepted

## Context
We need an event store that guarantees auditability, transactional integrity with the outbox pattern, and operational simplicity for a lean SRE team. Options considered: Marten atop PostgreSQL vs. EventStoreDB.

## Decision
Adopt Marten on PostgreSQL as the event store for Settlement Core.

## Financial and Operational Trade-offs
- Cost Efficiency:
  - PostgreSQL is ubiquitous, with managed offerings (RDS/Aurora, CloudSQL) and existing enterprise licenses/support.
  - EventStoreDB may imply specialized expertise and, at scale, dedicated clustering with higher TCO.
- Operational Simplicity:
  - One primary data platform (Postgres) reduces cognitive load: backups, HA, observability, security hardening.
  - Marten provides documents + events in one substrate; projections and outbox share a single transaction boundary.
- Performance & Scalability:
  - Append-only streams are efficient with proper partitioning and indexing; snapshotting keeps recovery O(1).
  - EventStoreDB is optimized for streams/reads but the marginal gain doesn’t offset ops complexity for our current SLA.
- Risk & Talent:
  - Postgres skills are widely available; Marten has healthy OSS velocity.
  - Avoids lock-in to specialized protocols while keeping escape hatches (CDC, logical replication).

## Consequences
- Pros: Lower TCO, simpler platform operations, ACID with outbox in one DB, easier cross-org adoption.
- Cons: Some specialized features (e.g., persistent subscriptions) require bespoke implementation or RabbitMQ.

## Alternatives Considered
- EventStoreDB: Excellent for high-stream cardinality and long retention with subscription tooling; higher ops overhead.
- CosmosDB/Change Feed: Cloud-specific, strong for projections; cost and lock-in concerns.
