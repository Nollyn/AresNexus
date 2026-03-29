using System.Text.Json;
using System.Collections.Concurrent;

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
    Task<IEnumerable<AIDecision>> GetDecisionsAsync(string agentId);
}

public interface IDecisionTraceStore
{
    Task StoreTraceAsync(string decisionId, object traceData);
}

public interface IAIModelRegistry
{
    Task<ModelInfo?> GetModelAsync(string modelId);
    Task RegisterModelAsync(ModelInfo modelInfo);
    Task<IEnumerable<ModelInfo>> GetAllModelsAsync();
}

public record ModelInfo(string Id, string Version, bool IsEnabled, string GovernancePolicy, DateTime RegisteredAt);

public class AgentAuditLogger : IAgentAuditLogger
{
    private readonly ConcurrentBag<AIDecision> _decisions = new();

    public async Task LogDecisionAsync(AIDecision decision)
    {
        _decisions.Add(decision);
        // In a real implementation, we would write to an immutable audit log (e.g., Marten/PostgreSQL)
        await Task.CompletedTask;
    }

    public Task<IEnumerable<AIDecision>> GetDecisionsAsync(string agentId)
    {
        return Task.FromResult(_decisions.Where(d => d.AgentId == agentId));
    }
}

public class AIModelRegistry : IAIModelRegistry
{
    private readonly ConcurrentDictionary<string, ModelInfo> _registry = new();

    public async Task<ModelInfo?> GetModelAsync(string modelId)
    {
        return await Task.FromResult(_registry.GetValueOrDefault(modelId));
    }

    public async Task RegisterModelAsync(ModelInfo modelInfo)
    {
        _registry[modelInfo.Id] = modelInfo;
        await Task.CompletedTask;
    }

    public Task<IEnumerable<ModelInfo>> GetAllModelsAsync()
    {
        return Task.FromResult<IEnumerable<ModelInfo>>(_registry.Values);
    }
}

public class DecisionTraceStore : IDecisionTraceStore
{
    private readonly ConcurrentDictionary<string, string> _traces = new();

    public async Task StoreTraceAsync(string decisionId, object traceData)
    {
        _traces[decisionId] = JsonSerializer.Serialize(traceData);
        await Task.CompletedTask;
    }
}
