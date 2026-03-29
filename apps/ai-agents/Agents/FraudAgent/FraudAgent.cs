using AresNexus.Settlement.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using AresNexus.AiAgents.Core;
using AresNexus.AiAgents.Core.Protection;
using AresNexus.AiAgents.Core.Governance;
using AresNexus.AiAgents.Core.DecisionGate;

namespace AresNexus.AiAgents.Agents.FraudAgent;

public class FraudAgent : BaseAgent
{
    private readonly IDecisionGate _decisionGate;

    public override string Name => "FraudAgent";
    public override string Description => "Analyzes transaction patterns to detect fraudulent activity.";

    public FraudAgent(Kernel kernel, ILogger<FraudAgent> logger, IDataProtectionGateway dataProtection, IAgentAuditLogger auditLogger, IDecisionGate decisionGate) 
        : base(kernel, logger, dataProtection, auditLogger)
    {
        _decisionGate = decisionGate;
    }

    public override async Task ProcessEventAsync(object @event, CancellationToken ct = default)
    {
        if (@event is FundsWithdrawnEvent withdrawal)
        {
            await AnalyzeWithdrawalAsync(withdrawal, ct);
        }
    }

    private async Task AnalyzeWithdrawalAsync(FundsWithdrawnEvent withdrawal, CancellationToken ct)
    {
        Logger.LogInformation("Analyzing withdrawal for fraud: {AccountId}, Amount: {Amount}", withdrawal.AccountId, withdrawal.Money.Amount);

        // DATA PROTECTION: Sanitize sensitive data before LLM
        var input = $"Account: {withdrawal.AccountId}, Amount: {withdrawal.Money.Amount} {withdrawal.Money.Currency}";
        var sanitizedInput = await DataProtection.SanitizeAsync(input);
        var inputHash = sanitizedInput.GetHashCode().ToString("X");

        // REASONING: Call LLM with sanitized data (simulated)
        var reasoning = withdrawal.Money.Amount > 50000 
            ? "High value transaction requires further investigation. Potential smurfing pattern."
            : "Standard transaction amount for this account profile.";
        var confidence = withdrawal.Money.Amount > 50000 ? 0.92 : 0.98;

        var recommendation = new RecommendationEvent(
            this.Name,
            withdrawal.Money.Amount > 50000 ? "BLOCK_TRANSACTION" : "PROCEED",
            reasoning,
            confidence,
            withdrawal.Money.Amount > 100000, // Requires human approval if > 100k
            "GPT-4-Swiss-v1",
            inputHash,
            new { withdrawal.AccountId, withdrawal.Money.Amount }
        );

        // DECISION GATE: Validate recommendation before emitting
        var isApproved = await _decisionGate.EvaluateRecommendationAsync(recommendation);

        if (isApproved && recommendation.RecommendationType == "BLOCK_TRANSACTION")
        {
             Logger.LogWarning("Fraud recommendation APPROVED. Emitting FraudRiskDetectedEvent.");
             // Emit recommendation to the event bus for the deterministic core to handle
             // await EventBus.PublishAsync(new FraudRiskDetectedEvent(withdrawal.AccountId, reasoning, "High", DateTime.UtcNow));
        }
    }
}
