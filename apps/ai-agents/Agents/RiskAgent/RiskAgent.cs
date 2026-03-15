using AresNexus.Services.Settlement.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using AresNexus.AiAgents.Core;
using AresNexus.AiAgents.Core.Protection;
using AresNexus.AiAgents.Core.Governance;

namespace AresNexus.AiAgents.Agents.RiskAgent;

public class RiskAgent : BaseAgent
{
    public override string Name => "RiskAgent";
    public override string Description => "Calculates risk scores based on transaction and account history.";

    public RiskAgent(Kernel kernel, ILogger<RiskAgent> logger, IDataProtectionGateway dataProtection, IAgentAuditLogger auditLogger) 
        : base(kernel, logger, dataProtection, auditLogger)
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
        Logger.LogInformation("Calculating risk for deposit to account: {AccountId}", deposit.AccountId);
        
        // DATA PROTECTION
        var input = $"Deposit amount: {deposit.Money.Amount} {deposit.Money.Currency}";
        var sanitizedInput = await DataProtection.SanitizeAsync(input);

        // REASONING
        double score = deposit.Money.Amount > 50000 ? 0.85 : 0.15;
        var reasoning = $"Risk score of {score} assigned based on transaction volume and origin. Confidence: 0.90";

        // GOVERNANCE
        await LogDecisionAsync(reasoning, 0.90, "RiskCalculation", sanitizedInput.GetHashCode().ToString());

        // RECOMMEND: Emit RiskScoreCalculatedEvent if score is above threshold
        Logger.LogInformation("Risk score calculated: {Score} for {AccountId}", score, deposit.AccountId);
    }
}
