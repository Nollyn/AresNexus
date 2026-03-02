using AresNexus.Settlement.Application.Interfaces;

namespace AresNexus.Settlement.Infrastructure.Services;

public sealed class SettlementService(IAccountRepository repository) : ISettlementService
{
    public async Task<bool> SettleAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default)
    {
        var account = await repository.GetByIdAsync(accountId, cancellationToken);
        if (account == null) return false;

        // Simple settlement logic for now (could be deposit or withdrawal)
        // For demonstration, let's assume it's always success if account exists
        return true;
    }
}
