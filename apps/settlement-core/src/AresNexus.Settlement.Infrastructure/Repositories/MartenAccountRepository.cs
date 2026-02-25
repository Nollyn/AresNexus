using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain.Aggregates;
using AresNexus.Settlement.Infrastructure.Messaging;
using Marten;
using Newtonsoft.Json;

namespace AresNexus.Settlement.Infrastructure.Repositories;

/// <summary>
/// Marten-based implementation of the Account repository.
/// </summary>
public sealed class MartenAccountRepository(IDocumentSession session, IEventStore eventStore) : IAccountRepository
{
    /// <inheritdoc />
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Load from snapshot if available
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

    /// <inheritdoc />
    public async Task SaveAsync(Account account, IEnumerable<object> outboxMessages, CancellationToken cancellationToken = default)
    {
        var changes = account.GetUncommittedChanges();
        if (changes.Count == 0) return;

        var expectedVersion = account.Version - changes.Count;

        // Start transactional operation in Marten
        // Append events to the aggregate stream
        session.Events.Append(account.Id, expectedVersion + 1, changes);

        // Crucial: Implement the Transactional Outbox
        // Extract _uncommittedEvents and save them into an OutboxMessages table in the same transaction.
        foreach (var msg in outboxMessages)
        {
            session.Store(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = msg.GetType().FullName ?? "Unknown",
                Content = JsonConvert.SerializeObject(msg),
                OccurredOnUtc = DateTime.UtcNow
            });
        }

        // Marten's DocumentSession handles the transaction across Events and Documents (OutboxMessages)
        await session.SaveChangesAsync(cancellationToken);

        // Optional: snapshotting could be moved here or kept in the handler
        // If snapshotting is required:
        if (account.Version >= 50 && expectedVersion / 50 < account.Version / 50)
        {
             await eventStore.SaveSnapshotAsync(account.Id, account.CreateSnapshot(), account.Version);
        }

        account.MarkChangesAsCommitted();
    }
}
