using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using AresNexus.AiAgents.Core.Protection;
using AresNexus.AiAgents.Core.Governance;

namespace AresNexus.AiAgents.Core;

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    Task ProcessEventAsync(object @event, CancellationToken ct = default);
}

public abstract class BaseAgent : IAgent
{
    protected readonly Kernel Kernel;
    protected readonly ILogger Logger;
    protected readonly IDataProtectionGateway DataProtection;
    protected readonly IAgentAuditLogger AuditLogger;

    protected BaseAgent(
        Kernel kernel,
        ILogger logger,
        IDataProtectionGateway dataProtection,
        IAgentAuditLogger auditLogger)
    {
        Kernel = kernel;
        Logger = logger;
        DataProtection = dataProtection;
        AuditLogger = auditLogger;
    }

    public abstract string Name { get; }
    public abstract string Description { get; }

    public abstract Task ProcessEventAsync(object @event, CancellationToken ct = default);

    protected async Task LogDecisionAsync(string reasoning, double confidence, string type, string inputHash)
    {
        var decision = new AIDecision(
            Guid.NewGuid(),
            DateTime.UtcNow,
            this.Name,
            "GPT-4-Swiss-v1", // Simulated model version
            inputHash,
            reasoning,
            confidence,
            type
        );

        await AuditLogger.LogDecisionAsync(decision);
    }
}
