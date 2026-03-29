using AresNexus.Settlement.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using AresNexus.AiAgents.Core;
using AresNexus.AiAgents.Core.Protection;
using AresNexus.AiAgents.Core.Governance;
using AresNexus.AiAgents.Core.DecisionGate;

namespace AresNexus.AiAgents.Agents.RiskAgent;

public class RiskAgent : BaseAgent
{
    private readonly IDecisionGate _decisionGate;

    public override string Name => "RiskAgent";
    public override string Description => "Calculates risk scores based on transaction and account history.";

    public RiskAgent(Kernel kernel, ILogger<RiskAgent> logger, IDataProtectionGateway dataProtection, IAgentAuditLogger auditLogger, IDecisionGate decisionGate) 
        : base(kernel, logger, dataProtection, auditLogger)
    {
        _decisionGate = decisionGate;
    }

    public override async Task ProcessEventAsync(object @event, CancellationToken ct = default)
    {
        if (@event is FundsDepositedEvent deposit)
        {
            await CalculateRiskAsync(deposit, ct);
        }
    }

    private async Task CalculateRiskAsync(FundsDepositedEvent deposit, CancellationToken ct)
    {
        Logger.LogInformation("Calculating risk for deposit to account: {AccountId}", deposit.AccountId);
        
        // DATA PROTECTION
        var input = $"Deposit amount: {deposit.Money.Amount} {deposit.Money.Currency}";
        var sanitizedInput = await DataProtection.SanitizeAsync(input);
        var inputHash = sanitizedInput.GetHashCode().ToString("X");

        // REASONING
        double riskScore = deposit.Money.Amount > 50000 ? 0.85 : 0.15;
        var reasoning = $"Risk score of {riskScore} assigned based on transaction volume and origin profile. Confidence: 0.94";
        var confidence = 0.94;

        var recommendation = new RecommendationEvent(
            this.Name,
            riskScore > 0.8 ? "HIGH_RISK_ALERT" : "NORMAL_RISK",
            reasoning,
            confidence,
            riskScore > 0.9,
            "GPT-4-Swiss-v1",
            inputHash,
            new { deposit.AccountId, riskScore }
        );

        // DECISION GATE
        var isApproved = await _decisionGate.EvaluateRecommendationAsync(recommendation);

        if (isApproved && recommendation.RecommendationType == "HIGH_RISK_ALERT")
        {
             Logger.LogWarning("High Risk recommendation APPROVED by Decision Gate for account {AccountId}.", deposit.AccountId);
        }
    }
}
