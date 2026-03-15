using AresNexus.Services.Settlement.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using AresNexus.AiAgents.Core;

namespace AresNexus.AiAgents.Agents.RiskAgent;

public record RiskScoreCalculatedEvent(Guid AccountId, double Score, string Reason);

public class RiskAgent : BaseAgent
{
    public override string Name => "Risk Management Agent";
    public override string Description => "Calculates risk scores based on transaction and account history.";

    public RiskAgent(Kernel kernel, ILogger<RiskAgent> logger) : base(kernel, logger)
    {
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
        Logger.LogInformation("Calculating risk for deposit to account: {AccountId}, Amount: {Amount}", deposit.AccountId, deposit.Money.Amount);
        
        // Example: If deposit is over 50k, it's higher risk
        double score = deposit.Money.Amount > 50000 ? 0.8 : 0.2;
        Logger.LogInformation("Calculated risk score: {Score} for {AccountId}", score, deposit.AccountId);
    }
}
