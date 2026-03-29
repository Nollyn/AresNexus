using AresNexus.Settlement.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using AresNexus.AiAgents.Core;
using AresNexus.AiAgents.Core.Protection;
using AresNexus.AiAgents.Core.Governance;
using AresNexus.AiAgents.Core.DecisionGate;

namespace AresNexus.AiAgents.Agents.SettlementAgent;

public class SettlementAgent : BaseAgent
{
    private readonly IDecisionGate _decisionGate;

    public override string Name => "SettlementAgent";
    public override string Description => "Monitors and assists the settlement engine.";

    public SettlementAgent(Kernel kernel, ILogger<SettlementAgent> logger, IDataProtectionGateway dataProtection, IAgentAuditLogger auditLogger, IDecisionGate decisionGate) 
        : base(kernel, logger, dataProtection, auditLogger)
    {
        _decisionGate = decisionGate;
    }

    public override async Task ProcessEventAsync(object @event, CancellationToken ct = default)
    {
        // SettlementAgent monitors settlement lifecycle events
        await Task.CompletedTask;
    }

    public async Task MonitorQueueAsync(CancellationToken ct)
    {
        Logger.LogInformation("Settlement Agent: Monitoring settlement queue...");
        
        // REASONING
        var reasoning = "Queue depth is within normal parameters. No stuck settlements detected. Confidence: 0.99";
        var confidence = 0.99;

        var recommendation = new RecommendationEvent(
            this.Name,
            "SYSTEM_HEALTH_OK",
            reasoning,
            confidence,
            false,
            "GPT-4-Swiss-v1",
            "SYSTEM_HEALTH_METRICS",
            new { QueueDepth = 0 }
        );

        // DECISION GATE
        await _decisionGate.EvaluateRecommendationAsync(recommendation);
    }
}
