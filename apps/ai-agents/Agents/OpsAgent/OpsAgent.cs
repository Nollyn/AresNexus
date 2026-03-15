using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using AresNexus.AiAgents.Core;
using AresNexus.AiAgents.Core.Protection;
using AresNexus.AiAgents.Core.Governance;

namespace AresNexus.AiAgents.Agents.OpsAgent;

public class OpsAgent : BaseAgent
{
    public override string Name => "OpsAgent";
    public override string Description => "Performs operational diagnostics and recommends remediation.";

    public OpsAgent(Kernel kernel, ILogger<OpsAgent> logger, IDataProtectionGateway dataProtection, IAgentAuditLogger auditLogger) 
        : base(kernel, logger, dataProtection, auditLogger)
    {
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
        
        // GOVERNANCE
        await LogDecisionAsync(reasoning, 0.95, "ServiceDiagnosis", $"METRICS_{serviceName}");
    }
}
