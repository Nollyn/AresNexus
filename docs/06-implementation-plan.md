# Technical Implementation Plan: Ares-Nexus

## 1. Modular Repository Structure
The project follows a **Monorepo Strategy** to maintain cross-service consistency while allowing independent deployment of each bounded context.

```text
/ares-nexus
├── /apps                     # Microservices (Bounded Contexts)
│   ├── /settlement-core      # .NET 10 Event Sourcing Engine
│   ├── /compliance-engine    # Python/Go AML Validation
│   └── /gateway-api          # Edge Validation & OIDC
├── /infrastructure           # Infrastructure as Code (IaC)
│   ├── /terraform            # Azure Resource Modules
│   └── /kubernetes           # Helm Charts & Network Policies
├── /shared                   # Shared Kernel (Contracts & Events)
│   ├── /events               # ISO 20022 / CloudEvent Schemas
│   └── /lib-resilience       # Shared Polly/Circuit Breaker Logic
└── /scripts                  # Automation & Chaos Engineering
