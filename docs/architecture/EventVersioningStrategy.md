# Event Versioning Strategy

## Overview
AresNexus follows an **Event Sourcing** pattern. Since events are immutable and represent the single source of truth, evolving their schema requires a robust versioning strategy to maintain backward compatibility and ensure replay safety.

## Versioning Rules
1.  **Immutability**: Once an event is persisted, it is never modified.
2.  **Schema Versioning**: Every `IDomainEvent` includes a `SchemaVersion` property.
3.  **Default Version**: New events start at version 1.
4.  **Semantic Changes**: If a change is additive (adding a nullable field), it may not require a version bump depending on the serializer settings. However, for Tier-1 banking, we prefer explicit versioning.
5.  **Breaking Changes**: Renaming fields or changing data types MUST result in a version bump.

## Backward Compatibility Approach
We use **Upcasting** to bridge the gap between old event schemas and the current domain model.

-   **Upcasters**: Small, focused classes that transform a legacy event into the next version.
-   **Chain of Responsibility**: Upcasters can be chained to move from V1 -> V2 -> V3.

## Migration Approach
-   **Lazy Migration (Upcasting)**: Events are transformed in-memory during the load process (when fetching from Marten). This avoids expensive "stop-the-world" data migrations.
-   **No In-place Updates**: We do not rewrite history. The store remains a pure append-only log.

## Replay Safety Guarantees
-   **Idempotency**: All upcasters must be pure functions. They transform data without side effects.
-   **Deterministic**: Replaying a stream of events at any point in time will result in the same aggregate state, regardless of when the events were originally produced.
-   **Validation**: Schema versions are validated during the upcasting process to ensure no version is skipped if explicit transitions are required.
