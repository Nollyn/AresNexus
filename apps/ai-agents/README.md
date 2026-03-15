# AresNexus AI Multi-Agent System

## Architecture Overview

The AresNexus AI Multi-Agent system is a distributed component designed to enhance the settlement platform with intelligent autonomous agents. It follows an **Observe → Reason → Act** pattern.

### Core Components

*   **Agent Runtime**: Orchestrates the execution of agents and manages their lifecycle.
*   **Decision Engine**: Uses Semantic Kernel to interact with LLMs for reasoning.
*   **LLM Provider**: A pluggable abstraction supporting OpenAI and local models (e.g., Ollama).

## Agents

### 1. Fraud Agent
Detects suspicious transaction patterns such as rapid repeats or abnormal bursts. It can emit `FraudAlertEvent` or request settlement halts.

### 2. Compliance Agent
Evaluates settlements against regulatory rules and can emit `ComplianceViolationEvent` or reject settlements.

### 3. Risk Agent
Calculates risk scores for transactions and accounts based on velocity and history.

### 4. Settlement Agent
Monitors the settlement queue, detects stuck settlements, and assists the engine by triggering retries.

### 5. Ops Agent
Performs operational diagnostics by inspecting logs and metrics to detect service failures.

### 6. Observability Agent
Analyzes telemetry data (Prometheus, logs) to detect system-wide anomalies like latency spikes or rising error rates.

## Event Flow

1.  **Observe**: Agents subscribe to domain events (e.g., `SettlementCreatedEvent`, `TransactionProcessedEvent`) and system metrics.
2.  **Reason**: Agents use the `DecisionEngine` (LLM-backed) to analyze the observation.
3.  **Act**: Based on reasoning, agents emit new events, trigger commands, or create diagnostic reports.

## Safety Rules

Agents are designed for **read-only observation** of the core financial state. They can only **recommend** actions or emit events. Critical mutations must always remain under the control of the core settlement engine.

## Observability

Agents expose Prometheus metrics:
*   `agent_decisions_total`
*   `agent_anomalies_detected`
*   `agent_actions_triggered`
