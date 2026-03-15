using AresNexus.Services.Settlement.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using AresNexus.AiAgents.Core;

namespace AresNexus.AiAgents.Agents.FraudAgent;

public record FraudAlertEvent(Guid AccountId, string Reason, string Severity, DateTime DetectedAt);

public class FraudAgent : BaseAgent
{
    public override string Name => "Fraud Detection Agent";
    public override string Description => "Analyzes transaction patterns to detect fraudulent activity.";

    public FraudAgent(Kernel kernel, ILogger<FraudAgent> logger) : base(kernel, logger)
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

        // Simple heuristic for demo purposes, in real world we'd use LLM reasoning
        if (withdrawal.Money.Amount > 10000)
        {
             Logger.LogWarning("High value withdrawal detected! Potential fraud: {AccountId}", withdrawal.AccountId);
             // In a real implementation, we would call the LLM here to reason about the account history
             // var decision = await Kernel.InvokePromptAsync("Is this transaction suspicious? ...");
             // emit FraudAlertEvent
        }
    }
}
