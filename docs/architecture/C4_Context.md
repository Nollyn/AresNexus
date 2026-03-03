# C4 Context Diagram - AresNexus

This diagram shows the AresNexus system in the context of its external actors and systems.

```mermaid
C4Context
    title System Context diagram for AresNexus

    Person(customer, "Banking Customer", "A customer who wants to perform financial transactions.")
    System_Ext(ext_bank, "External Bank", "External financial institution for cross-bank settlements.")
    
    System(ares_nexus, "AresNexus", "High-performance banking settlement core.")

    Rel(customer, ares_nexus, "Initiates transactions", "HTTPS/REST")
    Rel(ares_nexus, ext_bank, "Settles transactions with", "ISO 20022 / Swift")
    
    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

### Technology Mapping
- **AresNexus API:** ASP.NET Core 10.
- **External Interfaces:** RESTful API for customers, Message-based for banks.
- **Protocol:** TLS 1.3 for all external communication.
