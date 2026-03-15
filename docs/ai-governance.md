# AI Governance

This document describes the AI Governance framework implemented in the Ares Nexus platform, designed to comply with Swiss financial regulations (nFADP) and FINMA guidelines.

## Principles

1.  **Advisory Role Only**: AI agents never mutate financial state directly. They produce recommendations that must pass through the Decision Gate.
2.  **Full Auditability**: Every decision made by an AI agent is recorded with full traceability.
3.  **Data Sovereignty**: Sensitive data is sanitized before any interaction with LLM providers.
4.  **Human-in-the-loop**: Critical operations require explicit human approval.

## Governance Layer Components

### AI Model Registry
Maintains a catalog of authorized AI models and their versions. Only models listed in the registry can be used in production.

### Agent Audit Logger
Captures every AI interaction, including:
- `timestamp`: When the decision occurred.
- `agentId`: The agent that made the recommendation.
- `modelVersion`: The specific model version used.
- `inputHash`: A hash of the sanitized input data.
- `reasoningSummary`: The logic used by the agent.
- `confidenceScore`: The model's own assessment of its recommendation accuracy.

### Decision Gate
A central policy enforcement point that evaluates agent recommendations against business rules and regulatory constraints before they can be acted upon by the core system.
