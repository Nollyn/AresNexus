using AresNexus.Settlement.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using AresNexus.AiAgents.Core;
using AresNexus.AiAgents.Core.Protection;
using AresNexus.AiAgents.Core.Governance;
using AresNexus.AiAgents.Core.DecisionGate;

namespace AresNexus.AiAgents.Agents.ComplianceAgent;

public class ComplianceAgent : BaseAgent
{
    private readonly IDecisionGate _decisionGate;

    public override string Name => "ComplianceAgent";
    public override string Description => "Enforces regulatory rules and compliance policies.";

    public ComplianceAgent(Kernel kernel, ILogger<ComplianceAgent> logger, IDataProtectionGateway dataProtection, IAgentAuditLogger auditLogger, IDecisionGate decisionGate) 
        : base(kernel, logger, dataProtection, auditLogger)
    {
        _decisionGate = decisionGate;
    }

    public override async Task ProcessEventAsync(object @event, CancellationToken ct = default)
    {
        if (@event is AccountCreatedEvent accountCreated)
        {
            await CheckComplianceAsync(accountCreated, ct);
        }
    }

    private async Task CheckComplianceAsync(AccountCreatedEvent accountCreated, CancellationToken ct)
    {
        Logger.LogInformation("Checking compliance for new account: {AccountId}", accountCreated.AccountId);
        
        // DATA PROTECTION: Sanitize sensitive data
        var sanitizedOwner = await DataProtection.SanitizeAsync(accountCreated.Owner);
        var inputHash = sanitizedOwner.GetHashCode().ToString("X");

        // REASONING: Verify identity against Swiss Sanction Lists (simulated)
        var reasoning = "KYC check passed for sanitized identity. No match found on SECO or international sanction lists.";
        var confidence = 0.99;

        var recommendation = new RecommendationEvent(
            this.Name,
            "KYC_APPROVE",
            reasoning,
            confidence,
            false,
            "GPT-4-Swiss-v1",
            inputHash,
            new { accountCreated.AccountId }
        );

        // DECISION GATE
        var isApproved = await _decisionGate.EvaluateRecommendationAsync(recommendation);

        if (isApproved)
        {
             Logger.LogInformation("Compliance approval verified for account {AccountId}.", accountCreated.AccountId);
        }
        else
        {
             Logger.LogWarning("Compliance recommendation REJECTED by Decision Gate for account {AccountId}.", accountCreated.AccountId);
        }
    }
}
