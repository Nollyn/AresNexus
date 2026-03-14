### AresNexus Observability Architecture

This document describes the observability stack used in the AresNexus solution to monitor the health and performance of the microservices.

#### 1. Stack Overview (Production-Grade)
The observability stack follows the **LGTM** (Loki, Grafana, Tempo, Mimir/Prometheus) pattern, orchestrated by **OpenTelemetry**:
- **Grafana**: The unified visualization layer.
- **Loki**: Log aggregation and search.
- **Tempo**: High-scale distributed tracing.
- **Prometheus**: Metrics storage and alerting.
- **OpenTelemetry (OTel) Collector**: The vendor-neutral proxy that receives, processes, and exports all signals.

#### 2. Signal Flow
1. **Instrumentation**: Services (.NET/Python) emit signals.
   - **.NET services**: Emit metrics via OpenTelemetry Prometheus Exporter (on `/metrics`) and OTLP Exporter.
   - **Python services**: Emit metrics via `prometheus-client` (on `/metrics`).
2. **Collection**:
   - **Prometheus** scrapes services directly for metrics (pull model).
   - **OTel Collector** receives traces and logs via OTLP (`http://otel-collector:4317`).
3. **Processing**: Collector adds resource attributes (e.g., `service.name`, `region`).
4. **Exporting**: 
   - Traces -> Tempo (via `otlp` exporter).
   - Logs -> Loki (via `loki` exporter).

#### 3. Service Monitoring
##### .NET Services (Gateway API, Settlement Service)
- **NuGet Packages**: `OpenTelemetry.Exporter.Prometheus.AspNetCore`, `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `Serilog.Sinks.OpenTelemetry`.
- **Metrics**: Exposed via `app.UseOpenTelemetryPrometheusScrapingEndpoint()` on `/metrics:8080`.
- **Traces**: Configured in `Program.cs` to export via OTLP to OTel Collector.
- **Logs**: Serilog is configured with an `OpenTelemetry` sink to send structured JSON logs to the OTel Collector.
- **Custom Metrics**: Use `System.Diagnostics.Metrics` (standard OTel implementation).

##### Python Services (Compliance Engine)
- Uses `prometheus-client` to expose metrics on `/metrics:8080`.
- Integrated into Prometheus scrape configuration.

#### 4. Prometheus & Alerting
Prometheus scrapes all services directly:
- `gateway-api:8080/metrics`
- `settlement-core-1:8080/metrics`
- `settlement-core-2:8080/metrics`
- `settlement-core-3:8080/metrics`
- `compliance-engine:8080/metrics`
- `otel-collector:8889/metrics` (for self-monitoring)

**Alerting rules (`rules.yml`):**
- `ServiceDown`: Triggers if any scrape target is unreachable.
- `HighSettlementLatency`: Triggers if P99 processing > 50ms.
- `HighErrorRate`: Triggers if failed transactions > 10% of total volume.
- `RegulatoryValidationFailure`: Warning for compliance rejections.

#### 5. Grafana Integration
- **Data Sources**: Prometheus, Loki, and Tempo are pre-provisioned.
- **Loki <-> Tempo**: Logs and traces are linked via `TraceId` for seamless correlation (click a log entry to see the corresponding trace).
- **Dashboards**: The `ares-nexus-dashboard.json` provides a high-level view of TPS, latency, and errors.

#### 6. Debugging in Production
1. **Check Collector Logs**: `docker logs otel-collector` to verify if signals are arriving.
2. **Explore Loki**: Use the Explore tab in Grafana to search for error logs.
3. **Trace Transaction**: Use `TraceId` from logs to visualize the full request path in Tempo.
4. **Targets**: Access `http://localhost:9090/targets` to check scrape health.
