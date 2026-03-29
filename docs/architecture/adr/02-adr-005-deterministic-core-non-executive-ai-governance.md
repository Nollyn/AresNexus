# ADR-005 — Deterministic Core with Non-Executive AI Governance

## Status
Proposed

## Context
The AresNexus system is designed for Tier-1 banking environments, requiring strict compliance with FINMA (Switzerland), DORA (EU), and GDPR regulations. The integration of Artificial Intelligence (AI) presents risks to system determinism and regulatory auditability.

## Decision
We implement a **Deterministic Core with Non-Executive AI Governance** architecture.

### Key Principles:
1.  **Deterministic Core**: The financial system's state changes are executed exclusively by the Settlement Core. State transitions are captured via Event Sourcing (Marten/PostgreSQL) and are strictly deterministic.
2.  **Non-Executive AI**: AI agents operate in an isolated Observability Layer. They consume events but cannot directly execute financial commands.
3.  **Decision Gate**: All AI recommendations must pass through a Decision Gate. This gate enforces deterministic policies (e.g., confidence thresholds, human-in-the-loop requirements) before any core action is triggered.
4.  **Data Protection Gateway**: Sensitive data (PII) is sanitized (redacted or tokenized) before reaching the AI layer. This ensures compliance with Swiss Bank Secrecy and GDPR.
5.  **Model Governance**: A dedicated layer manages model versions, audits all agent decisions, and stores reasoning traces for full transparency.
6.  **Model Risk Management**: Continuous monitoring for model drift and performance ensures that AI components remain within safe operating parameters.

## Consequences
*   **Safety**: Eliminates the risk of "hallucinating" agents executing unauthorized financial transactions.
*   **Compliance**: Provides a full audit trail for both deterministic and AI-assisted decisions, satisfying FINMA-grade auditability.
*   **Complexity**: Introduces additional latency for AI-assisted workflows due to the Decision Gate and sanitization steps.
*   **Maintainability**: Requires rigorous versioning of both code and AI models.

## Compliance Alignment
*   **FINMA**: Operational risk guidance is met by having a clear separation between execution and observation.
*   **DORA**: System resilience is enhanced by the non-executive nature of AI.
*   **GDPR**: Data Protection Gateway ensures PII is never exposed to LLMs without proper sanitization.
