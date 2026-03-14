using AresNexus.Services.Settlement.Application.Interfaces;
using AresNexus.Services.Settlement.Domain.Aggregates;

namespace AresNexus.Services.Settlement.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of the Account repository for testing purposes.
/// </summary>
public sealed class InMemoryAccountRepository(IEventStore eventStore) : IAccountRepository
{
    /// <inheritdoc />
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var (snapshot, snapshotVersion) = await eventStore.GetLatestSnapshotAsync<Account.Snapshot>(id);
        var account = new Account();
        
        if (snapshot != null)
        {
            account.LoadFromSnapshot(snapshot);
        }

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
        if (changes.Count == 0 && !outboxMessages.Any()) return;

        var expectedVersion = account.Version - changes.Count;
        
        // Use the event store to save events and outbox messages atomically (simulated)
        await eventStore.SaveChangesAsync(account.Id, changes, expectedVersion, outboxMessages);

        if (account.Version >= 50 && expectedVersion / 50 < account.Version / 50)
        {
             await eventStore.SaveSnapshotAsync(account.Id, account.CreateSnapshot(), account.Version);
        }

        account.MarkChangesAsCommitted();
    }
}
