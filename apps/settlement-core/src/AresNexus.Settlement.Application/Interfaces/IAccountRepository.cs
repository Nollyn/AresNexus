using AresNexus.Settlement.Domain.Aggregates;

namespace AresNexus.Settlement.Application.Interfaces;

/// <summary>
/// Repository for the Account aggregate.
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// Loads an account by its unique identifier.
    /// </summary>
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the account changes and its outbox messages in a single transaction.
    /// </summary>
    Task SaveAsync(Account account, IEnumerable<object> outboxMessages, CancellationToken cancellationToken = default);
}
