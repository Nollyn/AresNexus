using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain.Aggregates;
using AresNexus.Settlement.Infrastructure.Messaging;
using Marten;
using System.Text.Json;

namespace AresNexus.Settlement.Infrastructure.Repositories;

/// <summary>
/// Marten-based implementation of the Account repository for Swiss Tier-1 Banking.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MartenAccountRepository"/> class.
/// </remarks>
/// <param name="session">The Marten document session.</param>
/// <param name="eventStore">The event store for snapshots and history.</param>
public sealed class MartenAccountRepository(IDocumentSession session, IEventStore eventStore) : IAccountRepository
{
    /// <summary>
    /// Loads an account by its unique identifier, supporting snapshotting.
    /// </summary>
    /// <param name="id">The account identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The loaded account or null if not found.</returns>
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Load from snapshot if available (Performance requirement #4)
        var (snapshot, snapshotVersion) = await eventStore.GetLatestSnapshotAsync<Account.Snapshot>(id);
        var account = new Account();
        
        if (snapshot != null)
        {
            account.LoadFromSnapshot(snapshot);
        }

        // Fetch events since the last snapshot
        var history = await eventStore.GetEventsAsync(id, snapshotVersion);
        
        if (snapshot == null && history.Count == 0)
        {
            return null;
        }

        account.LoadsFromHistory(history);
        return account;
    }

    /// <summary>
    /// Saves the account aggregate and its uncommitted events into the Outbox in a single transaction.
    /// </summary>
    /// <param name="account">The account aggregate to save.</param>
    /// <param name="outboxMessages">Additional messages to be saved to the outbox.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveAsync(Account account, IEnumerable<object> outboxMessages, CancellationToken cancellationToken = default)
    {
        var changes = account.GetUncommittedChanges();
        if (changes.Count == 0 && !outboxMessages.Any()) return;

        var expectedVersion = account.Version - changes.Count;

        if (changes.Count > 0)
        {
            // Append events to the aggregate stream in Marten
            session.Events.Append(account.Id, expectedVersion + 1, changes);

            // Crucial: Implement the Transactional Outbox (Persistence requirement #1)
            // Extract uncommitted events and save them into an OutboxMessages table in the same transaction.
            foreach (var change in changes)
            {
                session.Store(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = change.GetType().AssemblyQualifiedName ?? change.GetType().FullName ?? "Unknown",
                    Content = JsonSerializer.Serialize(change),
                    OccurredOnUtc = DateTime.UtcNow
                });
            }
        }

        // Also save additional outbox messages if any
        foreach (var msg in outboxMessages)
        {
            session.Store(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = msg.GetType().AssemblyQualifiedName ?? msg.GetType().FullName ?? "Unknown",
                Content = JsonSerializer.Serialize(msg),
                OccurredOnUtc = DateTime.UtcNow
            });
        }

        // Marten's DocumentSession handles the transaction across Events and Documents (OutboxMessages)
        await session.SaveChangesAsync(cancellationToken);

        // Snapshotting (Performance requirement #4)
        // Take a snapshot every 50 events
        if (account.Version >= 50 && (expectedVersion / 50 < account.Version / 50))
        {
            await eventStore.SaveSnapshotAsync(account.Id, account.CreateSnapshot(), account.Version);
        }

        account.MarkChangesAsCommitted();
    }
}
