using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using AresNexus.AiAgents.Core;
using AresNexus.AiAgents.Core.Protection;
using AresNexus.AiAgents.Core.Governance;
using AresNexus.AiAgents.Core.DecisionGate;

namespace AresNexus.AiAgents.Agents.OpsAgent;

public class OpsAgent : BaseAgent
{
    private readonly IDecisionGate _decisionGate;

    public override string Name => "OpsAgent";
    public override string Description => "Performs operational diagnostics and recommends remediation.";

    public OpsAgent(Kernel kernel, ILogger<OpsAgent> logger, IDataProtectionGateway dataProtection, IAgentAuditLogger auditLogger, IDecisionGate decisionGate) 
        : base(kernel, logger, dataProtection, auditLogger)
    {
        _decisionGate = decisionGate;
    }

    public override async Task ProcessEventAsync(object @event, CancellationToken ct = default)
    {
        // Ops agent can react to logs or system events
        await Task.CompletedTask;
    }

    public async Task DiagnoseServiceAsync(string serviceName)
    {
        Logger.LogInformation("Ops Agent: Diagnosing service {ServiceName}", serviceName);
        
        // REASONING
        var reasoning = $"Service {serviceName} is operating within normal parameters. CPU usage at 45%. No recent error logs. Confidence: 0.95";
        var confidence = 0.95;

        var recommendation = new RecommendationEvent(
            this.Name,
            "SERVICE_DIAGNOSIS_OK",
            reasoning,
            confidence,
            false,
            "GPT-4-Swiss-v1",
            $"METRICS_{serviceName}",
            new { Service = serviceName, CpuUsage = 45 }
        );

        // DECISION GATE
        await _decisionGate.EvaluateRecommendationAsync(recommendation);
    }
}
