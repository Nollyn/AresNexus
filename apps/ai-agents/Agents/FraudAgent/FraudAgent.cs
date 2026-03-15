using AresNexus.Services.Settlement.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using AresNexus.AiAgents.Core;
using AresNexus.AiAgents.Core.Protection;
using AresNexus.AiAgents.Core.Governance;

namespace AresNexus.AiAgents.Agents.FraudAgent;

public record FraudAlertEvent(Guid AccountId, string Reason, string Severity, DateTime DetectedAt);

public class FraudAgent : BaseAgent
{
    public override string Name => "FraudAgent";
    public override string Description => "Analyzes transaction patterns to detect fraudulent activity.";

    public FraudAgent(Kernel kernel, ILogger<FraudAgent> logger, IDataProtectionGateway dataProtection, IAgentAuditLogger auditLogger) 
        : base(kernel, logger, dataProtection, auditLogger)
    {
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

        // REASONING: Call LLM with sanitized data (simulated here)
        var prompt = $"As a FraudAgent, analyze this transaction: {sanitizedInput}";
        
        // Simulating LLM response
        var reasoning = "High value transaction from a known account. Confidence: 0.95";
        var confidence = 0.95;

        // GOVERNANCE: Log the decision
        await LogDecisionAsync(reasoning, confidence, "FraudAnalysis", sanitizedInput.GetHashCode().ToString());

        if (withdrawal.Money.Amount > 10000)
        {
             Logger.LogWarning("High value withdrawal detected! Potential fraud recommendation emitted.");
             // RECOMMEND: Emit Recommendation Event only
             // await EventBus.PublishAsync(new FraudRiskDetectedEvent(withdrawal.AccountId, reasoning, "High", DateTime.UtcNow));
        }
    }
}
