using AresNexus.AiAgents.Core.Governance;
using AresNexus.AiAgents.Core.ModelRisk;

namespace AresNexus.AiAgents.Core.DecisionGate;

public record RecommendationEvent(
    string AgentId,
    string RecommendationType,
    string Details,
    double ConfidenceScore,
    bool RequiresHumanApproval,
    string ModelVersion,
    string InputHash,
    object Metadata
);

public interface IDecisionGate
{
    Task<bool> EvaluateRecommendationAsync(RecommendationEvent recommendation);
}

public class DecisionGate : IDecisionGate
{
    private readonly IAgentAuditLogger _auditLogger;
    private readonly IModelRiskManager _riskManager;

    public DecisionGate(IAgentAuditLogger auditLogger, IModelRiskManager riskManager)
    {
        _auditLogger = auditLogger;
        _riskManager = riskManager;
    }

    public async Task<bool> EvaluateRecommendationAsync(RecommendationEvent recommendation)
    {
        // 1. Audit Log the receipt of recommendation
        await _auditLogger.LogDecisionAsync(new AIDecision(
            Guid.NewGuid(),
            DateTime.UtcNow,
            recommendation.AgentId,
            recommendation.ModelVersion,
            recommendation.InputHash,
            recommendation.Details,
            recommendation.ConfidenceScore,
            recommendation.RecommendationType
        ));

        // 2. Model Risk Management Check
        var isModelSafe = await _riskManager.ValidateModelDecisionAsync(recommendation.AgentId, recommendation);
        if (!isModelSafe)
        {
            return false;
        }

        // 3. Enforce Deterministic Policies
        if (recommendation.ConfidenceScore < 0.85) // Tier-1 banking threshold
        {
            return false;
        }

        if (recommendation.RequiresHumanApproval)
        {
            // Human-in-the-loop requirement for certain categories of actions
            return false; 
        }

        // 4. Verification against compliance rules (if possible)
        
        return true;
    }
}
