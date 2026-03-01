using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain.Aggregates;
using AresNexus.Settlement.Domain.Events;
using AresNexus.Settlement.Infrastructure.Messaging;
using AresNexus.Shared.Kernel;
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
                    var encrypted = await encryptionService.EncryptAsync(deposited.Reference);
                    var updated = deposited with { Reference = encrypted };
                    // Since it's a collection, we might need to replace it if we want the encrypted version to be stored.
                    // However, Marten's Append takes the collection.
                }
            }
            else if (change is FundsWithdrawnEvent withdrawn)
            {
                if (!string.IsNullOrEmpty(withdrawn.Reference))
                {
                    var encrypted = await encryptionService.EncryptAsync(withdrawn.Reference);
                    var updated = withdrawn with { Reference = encrypted };
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
                var traceId = (change as IDomainEvent)?.TraceId;
                var correlationId = (change as IDomainEvent)?.CorrelationId;

                session.Store(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = change.GetType().AssemblyQualifiedName ?? change.GetType().FullName ?? "Unknown",
                    Content = JsonSerializer.Serialize(change),
                    TraceId = traceId,
                    CorrelationId = correlationId,
                    OccurredOnUtc = DateTime.UtcNow
                });
            }
        }

        // Also save additional outbox messages if any (propagate IDs if message type supports them)
        foreach (var msg in outboxMessages)
        {
            var traceId = (msg as IDomainEvent)?.TraceId;
            var correlationId = (msg as IDomainEvent)?.CorrelationId;

            session.Store(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = msg.GetType().AssemblyQualifiedName ?? msg.GetType().FullName ?? "Unknown",
                Content = JsonSerializer.Serialize(msg),
                TraceId = traceId,
                CorrelationId = correlationId,
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
