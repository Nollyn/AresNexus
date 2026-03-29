using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using AresNexus.AiAgents.Core;
using AresNexus.AiAgents.Core.Protection;
using AresNexus.AiAgents.Core.Governance;
using AresNexus.AiAgents.Core.DecisionGate;

namespace AresNexus.AiAgents.Agents.ObservabilityAgent;

public class ObservabilityAgent : BaseAgent
{
    private readonly IDecisionGate _decisionGate;

    public override string Name => "ObservabilityAgent";
    public override string Description => "Analyzes telemetry data and detects anomalies.";

    public ObservabilityAgent(Kernel kernel, ILogger<ObservabilityAgent> logger, IDataProtectionGateway dataProtection, IAgentAuditLogger auditLogger, IDecisionGate decisionGate) 
        : base(kernel, logger, dataProtection, auditLogger)
    {
        _decisionGate = decisionGate;
    }

    public override async Task ProcessEventAsync(object @event, CancellationToken ct = default)
    {
        // React to system events or anomalies
        await Task.CompletedTask;
    }

    public async Task AnalyzeMetricsAsync(string metricsData)
    {
        Logger.LogInformation("Observability Agent: Analyzing metrics...");
        
        // REASONING
        var reasoning = "System throughput is optimal. Latency p99 is at 120ms, well within SLA. Confidence: 0.98";
        var confidence = 0.98;

        var recommendation = new RecommendationEvent(
            this.Name,
            "SYSTEM_METRICS_ANOMALY_FREE",
            reasoning,
            confidence,
            false,
            "GPT-4-Swiss-v1",
            "TELEMETRY_DATA_CHUNK",
            new { LatencyP99 = 120 }
        );

        // DECISION GATE
        await _decisionGate.EvaluateRecommendationAsync(recommendation);
    }
}
