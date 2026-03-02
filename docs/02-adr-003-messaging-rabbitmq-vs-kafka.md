# ADR 003: RabbitMQ vs. Kafka for Inter-service Messaging

## Status
Accepted

## Context
Settlement requires low-latency command processing with strong delivery guarantees and routing patterns (dead-lettering, retries), but does not require multi-day stream retention for analytics within the core.

## Decision
Use RabbitMQ for command and integration message delivery between services.

## Financial and Operational Trade-offs
- Latency Requirements:
  - RabbitMQ provides sub-10ms broker latencies in typical deployments, aligning with <50ms p99 end-to-end.
  - Kafka excels at high-throughput streaming and long retention; consumer group lag adds complexity for command workflows.
- Operational Footprint:
  - RabbitMQ clustering and HA queues are straightforward for small SRE teams.
  - Kafka requires Zookeeper/Kraft, partition planning, and higher baseline resource consumption.
- Delivery Semantics:
  - RabbitMQ supports at-least-once with fine-grained NACK/requeue, DLQs, and per-queue policies.
  - Exactly-once on Kafka often implies idempotent producers and careful consumer design; we already enforce idempotency at handlers.
- Cost:
  - Lower infra/TCO at target scale; simpler to secure and monitor.

## Consequences
- Pros: Lower latency for settlement commands, rich routing/topology, easier operations.
- Cons: Not ideal for long-term analytics retention; complement with data lake or CDC for analytics.

## Alternatives Considered
- Kafka: Superior for event streaming, replay, and long-term retention; operationally heavier for command-style messaging.
