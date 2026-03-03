# C4 Component Diagram - Settlement API

This diagram shows the core components inside the Settlement API.

```mermaid
C4Component
    title Component diagram for Settlement API

    Container(user, "User/External System", "Client initiating requests.")
    
    System_Boundary(api_boundary, "Settlement API") {
        Component(handler, "Transaction Handler", "MediatR Command Handler", "Processes business commands.")
        Component(idempotency, "Idempotency Guard", "MediatR Pipeline Behavior", "Ensures commands are processed only once.")
        Component(eventstore, "Event Store Adapter", "Marten / IEventStore", "Appends events to streams.")
        Component(outbox, "Outbox Processor", "Background Service", "Dispatches outbox messages.")
        Component(obs, "Observability Pipeline", "OpenTelemetry", "Exports traces and metrics.")
    }

    ContainerDb(db, "PostgreSQL", "Marten Store", "Physical event and document storage.")

    Rel(user, idempotency, "Sends Command", "HTTPS")
    Rel(idempotency, handler, "Passes verified command", "In-process")
    Rel(handler, eventstore, "Appends events", "In-process")
    Rel(eventstore, db, "Saves (Transaction)", "Npgsql")
    Rel(outbox, db, "Queries outbox", "Npgsql")
    Rel(handler, obs, "Records metrics", "SDK")
```

### Components Details
- **Idempotency Guard:** Uses `CommandId` to prevent duplicate processing.
- **Transaction Handler:** Implements the business logic of the settlement domain.
- **Event Store Adapter:** Translates domain events into Marten-compatible storage operations.
- **Outbox Processor:** Ensures at-least-once delivery of integration events.
- **Observability Pipeline:** Provides P95 latency metrics and full transaction tracing.
