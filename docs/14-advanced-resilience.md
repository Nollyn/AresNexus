# Advanced Resilience: Snapshotting and Upcasting

## Overview

To meet the stringent requirements of Swiss Tier-1 Banking (FINMA/DORA), the AresNexus Settlement Engine implements advanced persistence and resilience patterns. These patterns ensure the system remains performant, scalable, and compliant over decades of operation.

## Snapshotting Mechanism

As the number of events in an aggregate's stream grows, the time required to replay those events to reconstruct the current state increases. To maintain sub-millisecond latency for high-frequency trading, we implement a **Snapshotting Mechanism**.

### Implementation Details

1.  **Infrastructure Entity**: A dedicated `Snapshot` entity is stored in the `AresNexus.Settlement.Infrastructure` layer. This entity persists a serialized version of the aggregate's state at a specific version.
2.  **Frequency**: A snapshot is automatically generated every **100 events**. This bounds the maximum number of events to replay to 99, ensuring consistent performance regardless of the total history length.
3.  **Loading Logic**: When loading an aggregate:
    -   The system first retrieves the latest available snapshot from the `Snapshot` table.
    -   If a snapshot exists, the aggregate is initialized with the snapshotted state.
    -   Only events with a version greater than the snapshotted version are replayed from the Event Store.

## Event Upcasting

Data immutability is a core requirement for regulatory compliance. However, domain models evolve over time. **Event Upcasting** allows us to transform legacy events into current versions during the deserialization process without modifying the original, immutable event store.

### How it Works

1.  **IEventUpcaster**: An interface defined in the infrastructure layer that identifies and transforms specific event types.
2.  **Versioning**: When an event schema changes (e.g., adding a `Currency` field to `FundsDepositedEvent`), we create a new version of the event record.
3.  **On-the-fly Transformation**: During event retrieval, the `MartenEventStore` passes events through registered upcasters. For example, `MoneyDeposited_v1_to_v2_Upcaster` detects `v1` events and adds a default currency (e.g., "CHF") to produce a `v2` event compatible with the current domain logic.

## Compliance and Resilience

-   **Performance**: Snapshotting prevents "event stream bloat" from degrading system responsiveness.
-   **Evolution**: Upcasting ensures that data recorded today remains readable and relevant for decades, even as the business logic evolves.
-   **Traceability**: Combined with Distributed Tracing (OpenTelemetry), every state change is fully auditable, meeting FINMA's rigorous standards for transparency and resilience.
