# ADR 004: Minimal APIs vs. MVC Controllers

## Status
Accepted

## Context
We expose synchronous validation and command endpoints with stringent latency SLOs and frequent scale events (auto-scale, cold starts during rollouts). Traditional MVC Controllers add indirection and reflection-based binding overhead.

## Decision
Adopt **Minimal APIs** and **.NET 10** for the Settlement Core HTTP surface.

## Business Rationale (Executive Summary)
- **Optimized for TCO (Total Cost of Ownership)**: Leveraging .NET 10 with **Chiseled Containers** reduces image sizes by ~80%, lowering storage and egress costs while significantly reducing the attack surface for security vulnerabilities (Trivy scan compliant).
- **Extreme High-Density Scaling**: Minimal APIs provide a high-frequency execution profile with reduced memory footprint, allowing for 2x more replica instances on the same hardware compared to MVC, directly lowering cloud operational costs.
- **DORA Resilience Compliance**: Faster "cold starts" and reduced application boot time ensure that in the event of a cluster-wide failure, the settlement engine recovers in < 30s, meeting our MTTR SLAs and Swiss regulatory benchmarks.

## Financial and Operational Trade-offs
- Performance & Cold Start:
  - Minimal APIs reduce middleware and binding overhead; faster cold starts and lower per-request CPU, aiding p99 latency.
  - Controllers provide richer conventions but add overhead not required for our bounded HTTP surface.
- Simplicity & Developer Velocity:
  - Route handlers are concise, AOT-friendly, and integrate cleanly with DI/filters.
  - Some advanced features (filters, model binding) require explicit composition—acceptable trade for speed.
- Scaling Behavior:
  - Lean handlers enable more pods per node at same SLO, improving cost efficiency under burst traffic.

## Consequences
- Pros: Lower latency, simpler hosting model, better fit for serverless/fast autoscale.
- Cons: Less structure out-of-the-box; requires discipline for larger surfaces (we compensate with Architecture Tests).

## Alternatives Considered
- MVC Controllers: Familiar patterns and filters; higher overhead and longer cold starts vs our SLOs.
