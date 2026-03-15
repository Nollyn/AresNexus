# Multi-Agent Architecture

The AI-agent ecosystem in Ares Nexus is designed to provide secure, regulatory-compliant intelligence to the financial core.

## Agents

- **FraudAgent**: Detects suspicious transaction patterns.
- **ComplianceAgent**: Enforces KYC/AML rules and Swiss regulatory compliance.
- **RiskAgent**: Calculates exposure and operational risks.
- **SettlementAgent**: Monitors and assists the settlement engine.
- **OpsAgent**: Performs system diagnostics and suggests remediation.
- **ObservabilityAgent**: Analyzes telemetry for anomalies.

## Design Pattern: Observe → Reason → Recommend

Agents never execute commands. They follow a three-step cycle:
1.  **Observe**: Ingest system events and metrics via the event bus.
2.  **Reason**: Use Large Language Models (LLMs) to analyze observations after data sanitization.
3.  **Recommend**: Produce a recommendation event containing reasoning and confidence.

## Data Protection Gateway

Before an agent interacts with an LLM, all data passes through the **Data Protection Gateway (DPG)**:
- **Redaction**: PII like names and emails are removed or replaced.
- **Hashing**: IBANs and Account IDs are irreversibly hashed.
- **Bucketing**: Exact financial amounts are converted to ranges (e.g., CHF 1,000-5,000).

This ensures that sensitive financial data never leaves the system's secure zone.
