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
    /// <param name="id">The unique identifier of the account.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded account aggregate or null if not found.</returns>
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the account changes and its outbox messages in a single transaction.
    /// </summary>
    /// <param name="account">The account aggregate to save.</param>
    /// <param name="outboxMessages">The collection of outbox messages to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveAsync(Account account, IEnumerable<object> outboxMessages, CancellationToken cancellationToken = default);
}
