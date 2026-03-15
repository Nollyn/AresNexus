using System.Text.Json;

namespace AresNexus.AiAgents.Core.Governance;

public record AIDecision(
    Guid Id,
    DateTime Timestamp,
    string AgentId,
    string ModelVersion,
    string InputHash,
    string ReasoningSummary,
    double ConfidenceScore,
    string DecisionType
);

public interface IAgentAuditLogger
{
    Task LogDecisionAsync(AIDecision decision);
}

public interface IDecisionTraceStore
{
    Task StoreTraceAsync(string decisionId, object traceData);
}

public interface IAIModelRegistry
{
    Task<ModelInfo?> GetModelAsync(string modelId);
    Task RegisterModelAsync(ModelInfo modelInfo);
}

public record ModelInfo(string Id, string Version, bool IsEnabled, string GovernancePolicy);

public class AgentAuditLogger : IAgentAuditLogger
{
    private readonly List<AIDecision> _decisions = new();

    public async Task LogDecisionAsync(AIDecision decision)
    {
        _decisions.Add(decision);
        // In a real implementation, we would write to an immutable audit log or specialized store
        await Task.CompletedTask;
    }
}

public class AIModelRegistry : IAIModelRegistry
{
    private readonly Dictionary<string, ModelInfo> _registry = new();

    public async Task<ModelInfo?> GetModelAsync(string modelId)
    {
        return await Task.FromResult(_registry.GetValueOrDefault(modelId));
    }

    public async Task RegisterModelAsync(ModelInfo modelInfo)
    {
        _registry[modelInfo.Id] = modelInfo;
        await Task.CompletedTask;
    }
}
