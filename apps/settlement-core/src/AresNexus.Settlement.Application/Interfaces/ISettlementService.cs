namespace AresNexus.Settlement.Application.Interfaces;

/// <summary>
/// Interface for Settlement Service to handle high-level settlement logic.
/// </summary>
public interface ISettlementService
{
    Task<bool> SettleAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default);
}
