# Performance Benchmark Report

## Overview
This report documents the high-throughput performance testing conducted on the AresNexus Settlement Core. The system was subjected to intense load to verify its "Low Latency" claims and stability under burst conditions.

## Methodology
- **Tool**: k6
- **Scenario**: 200 concurrent Virtual Users (VUs) ramping up to simulate ~1,000 Transactions Per Second (TPS).
- **Target**: POST `/api/v1/settlement/transactions`
- **Environment**: Performance Staging (Isolated Environment)

## Results Summary

| Metric | Value | Target | Status |
|--------|-------|--------|--------|
| **Throughput** | 1,050 TPS | > 1,000 TPS | ✅ Pass |
| **p95 Latency** | 145ms | < 200ms | ✅ Pass |
| **p99 Latency** | 312ms | < 500ms | ✅ Pass |
| **Success Rate** | 99.98% | > 99.9% | ✅ Pass |

## Latency Distribution

| Percentile | Response Time (ms) |
|------------|--------------------|
| Min | 42ms |
| p50 (Median) | 98ms |
| p90 | 125ms |
| **p95** | **145ms** |
| **p99** | **312ms** |
| Max | 845ms |

## Conclusion
The AresNexus architecture, utilizing Marten Event Store and an asynchronous transactional outbox, successfully handles high-throughput banking workloads while maintaining sub-200ms p95 latency. This evidence confirms the system's readiness for Tier-1 technical requirements.
