# C4 Model: Ares-Nexus System Architecture

## 1. Level 1: System Context Diagram
**Description:** This diagram defines the scope of Ares-Nexus within the Swiss Financial Ecosystem. It shows how the system interacts with external actors and regulatory bodies.

```mermaid
graph TD
    User[Commercial Bank / Client] -->|ISO 20022 Instructions| APIGateway[Ares-Nexus Gateway]
    APIGateway -->|Events| Settlement[Settlement Core]
    Settlement -->|Settlement Confirmation| SIC[Swiss Interbank Clearing - RTGS]
    Settlement -->|Immutable Logs| FINMA[FINMA Regulatory Portal]
    Settlement -->|Query Data| ReadModel[(Read Model Database)]
