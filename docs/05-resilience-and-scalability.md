
---

### File 5: `05-resilience-and-scalability.md`

```markdown
# Proof of Resilience: Meeting the 99.99% Availability Constraint

## 1. High Availability (HA) Strategy
*   **Multi-Region Active-Passive:** The system is deployed in `switzerlandnorth` (Primary) and `switzerlandwest` (Secondary). 
*   **Automated Failover:** Using **Azure Front Door** to detect regional outages and reroute traffic in <30 seconds.

## 2. Latency Optimization
*   **Edge Validation:** ISO 20022 schemas are validated at the Gateway level to prevent "Junk Traffic" from reaching the Core.
*   **In-Memory Projections:** The Read Model uses **Redis** for the most frequent account balance lookups, achieving sub-5ms response times.

## 3. Chaos Engineering (Verification)
To justify seniority, I have implemented a **Chaos Mesh** suite that simulates:
*   **Pod Eviction:** Ensuring the system recovers without losing a single financial event.
*   **Latency Injection:** Testing the **Circuit Breaker** patterns to ensure the UI remains responsive even if the DB is slow.