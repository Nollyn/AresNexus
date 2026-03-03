# C4 Architecture Overview

This document provides a hierarchical view of the AresNexus architecture, following the C4 Model.

## [Level 1: System Context](diagrams/C4_Context.mermaid)
The high-level relationship between AresNexus and its external ecosystem.
- **Key Takeaway:** AresNexus acts as the central settlement engine, interacting with external clients and maintaining strict trust boundaries.

## [Level 2: Container Diagram](diagrams/C4_Container.mermaid)
The decomposition of the system into logical containers and data stores.
- **Key Takeaway:** The API is designed for stateless horizontal scaling, with PostgreSQL (Marten) serving as the primary consistency anchor through event sourcing.

## [Level 3: Component Diagram](diagrams/C4_Component.mermaid)
The internal structure of the API container, showing how commands are processed and how resilience is enforced.
- **Key Takeaway:** The command path is heavily protected by rate limiting and Polly policies, ensuring stability even under stress.

## Architectural Principles Illustrated
1. **Event Sourcing:** All state changes are persisted as a sequence of immutable events in Marten.
2. **Transactional Outbox:** External messaging is integrated into the primary database transaction to ensure eventual consistency.
3. **Resilience by Design:** Circuit breakers and rate limiters are first-class citizens in the request pipeline.
4. **Stateless Scaling:** All session-related or temporary state is moved to Redis or the database, allowing any API instance to handle any request.
