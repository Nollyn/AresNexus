# C4 Container Diagram - AresNexus

This diagram shows the internal containers of the AresNexus system.

```mermaid
C4Container
    title Container diagram for AresNexus

    Person(user, "User/Client", "External consumer of the API.")

    System_Boundary(c1, "AresNexus Settlement Core") {
        Container(api, "Settlement API", "ASP.NET Core 10", "Provides REST endpoints for settlement operations.")
        Container(worker, "Outbox Processor", "Background Service", "Processes the transactional outbox and publishes events.")
        ContainerDb(db, "Event Store & DB", "PostgreSQL (Marten)", "Stores immutable event streams and projections.")
    }

    System_Ext(broker, "Message Broker", "Azure Service Bus / RabbitMQ", "External message broker for event distribution.")

    Rel(user, api, "Uses", "HTTPS/JSON")
    Rel(api, db, "Reads/Writes", "Npgsql")
    Rel(worker, db, "Reads outbox", "Npgsql")
    Rel(worker, broker, "Publishes events to", "AMQP/SBMP")
```

### Data Flow
1. **API** receives a request and creates a command.
2. **API** saves the resulting events and an outbox entry to **PostgreSQL** in a single transaction.
3. **Outbox Processor** polls **PostgreSQL** for new outbox entries.
4. **Outbox Processor** publishes the messages to the **Message Broker**.
