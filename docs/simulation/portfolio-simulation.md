# Ares-Nexus: Institutional Portfolio Simulation

This document describes the synthetic portfolio simulation environment used to demonstrate the Ares-Nexus architecture at scale.

## Overview

The simulation environment generates a deterministic synthetic dataset representing an institutional investment portfolio of **$5.5 Billion USD**. This dataset is used to test system throughput, AI agent observability, and deterministic settlement execution without requiring real financial data.

## Portfolio Characteristics

- **Total Value:** $5.5B USD
- **Accounts:** 50,000 Institutional/Corporate accounts.
- **Positions:** 200,000 diversified positions.
- **Asset Allocation:**
  - Equities: 40%
  - Bonds: 30%
  - Derivatives: 10%
  - FX: 10%
  - Cash: 10%

## Architecture of the Simulation

The simulation follows a 2-stage process:

1.  **Synthetic Portfolio Generator:** Creates a reproducible set of accounts, positions, and pending transactions using a fixed random seed.
2.  **Transaction Simulator:** Streams transactions from the dataset into the **Gateway API**, simulating realistic trading activity.

## How to Run

To execute the simulation, use the following command from the root of the repository:

```bash
make demo-portfolio
```

This command will:
1.  Compile and run the `AresNexus.Simulation` tool.
2.  Generate `simulation/data/*.json` files.
3.  Simulate a stream of transactions to the Gateway API (Port 5001).

## Determinism and Reproducibility

The simulation uses a fixed seed (`42`) to ensure that every execution produces the exact same portfolio structure and transaction sequence. This is critical for:
- Benchmarking performance improvements.
- Verifying AI agent behavior across different model versions.
- Auditing system state after specific transaction bursts.

## Metrics and Observability

During the simulation, the system emits high-resolution metrics to **Prometheus**. You can monitor the simulation in real-time via the **Grafana Dashboards**:

- **Settlement Throughput:** Transactions per second (TPS).
- **Processing Latency:** Time from Gateway entry to Event Store persistence.
- **AI Recommendations:** Number of signals produced by agents per transaction.
- **Decision Gate Approvals:** Validated vs. Rejected agent signals.

## Compliance and Privacy

All synthetic data follows the same compliance rules as real data:
- **PII Redaction:** Account owners are synthetic but treated as PII by the Compliance Engine.
- **Swiss Bank Secrecy:** Data is tokenized and encrypted before reaching the AI Observability Layer.
- **Auditability:** Every transaction in the simulation is recorded in the immutable Event Store.
