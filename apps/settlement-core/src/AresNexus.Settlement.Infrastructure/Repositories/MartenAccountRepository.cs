using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain.Aggregates;
using AresNexus.Settlement.Domain.Events;
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
/// <param name="encryptionService">The PII encryption service.</param>
public sealed class MartenAccountRepository(IDocumentSession session, IEventStore eventStore, IEncryptionService encryptionService) : IAccountRepository
{
    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task SaveAsync(Account account, IEnumerable<object> outboxMessages, CancellationToken cancellationToken = default)
    {
        var changes = account.GetUncommittedChanges();
        if (changes.Count == 0 && !outboxMessages.Any()) return;

        // Encrypt PII fields before serialization (Security requirement #4)
        foreach (var change in changes)
        {
            if (change is FundsDepositedEvent deposited)
            {
                if (!string.IsNullOrEmpty(deposited.Reference))
                {
                    var field = typeof(FundsDepositedEvent).GetField("<Reference>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (field != null)
                    {
                        var encrypted = await encryptionService.EncryptAsync(deposited.Reference);
                        field.SetValue(deposited, encrypted);
                    }
                }
            }
            else if (change is FundsWithdrawnEvent withdrawn)
            {
                if (!string.IsNullOrEmpty(withdrawn.Reference))
                {
                    var field = typeof(FundsWithdrawnEvent).GetField("<Reference>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (field != null)
                    {
                        var encrypted = await encryptionService.EncryptAsync(withdrawn.Reference);
                        field.SetValue(withdrawn, encrypted);
                    }
                }
            }
        }

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
        // Take a snapshot every 100 events
        if (account.Version >= 99 && (expectedVersion / 100 < account.Version / 100))
        {
            await eventStore.SaveSnapshotAsync(account.Id, account.CreateSnapshot(), account.Version);
        }

        account.MarkChangesAsCommitted();
    }
}
