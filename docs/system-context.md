graph TD
    User[Commercial Bank / Client] -->|ISO 20022 Instructions| APIGateway[Ares-Nexus Gateway]
    APIGateway -->|Events| Settlement[Settlement Core]
    Settlement -->|Settlement Confirmation| SIC[Swiss Interbank Clearing - RTGS]
    Settlement -->|Immutable Logs| FINMA[FINMA Regulatory Portal]
    Settlement -->|Query Data| ReadModel[(Read Model Database)]
