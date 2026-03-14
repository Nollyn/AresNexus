using AresNexus.Services.Settlement.Application.Interfaces;
using AresNexus.Services.Settlement.Domain.Aggregates;
using AresNexus.Services.Settlement.Domain.Events;
using AresNexus.Services.Settlement.Infrastructure.Messaging;
using AresNexus.Services.Settlement.Infrastructure.Resilience;
using AresNexus.BuildingBlocks.Domain;
using Marten;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace AresNexus.Services.Settlement.Infrastructure.Repositories;

/// <summary>
/// Marten-based implementation of the Account repository for Swiss Tier-1 Banking.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MartenAccountRepository"/> class.
/// </remarks>
/// <param name="session">The Marten document session.</param>
/// <param name="eventStore">The event store for snapshots and history.</param>
/// <param name="encryptionService">The PII encryption service.</param>
/// <param name="configuration">The configuration for snapshots.</param>
/// <param name="resiliencePolicyFactory">The resilience policy factory.</param>
public sealed class MartenAccountRepository(
    IDocumentSession session, 
    IEventStore eventStore, 
    IEncryptionService encryptionService,
    IConfiguration configuration,
    IResiliencePolicyFactory resiliencePolicyFactory) : IAccountRepository
{
    private readonly int _snapshotInterval = configuration.GetValue<int>("EventSourcing:SnapshotInterval", 100);
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
        await resiliencePolicyFactory.GetDatabasePolicy().ExecuteAsync(async () => 
        {
            var changes = account.GetUncommittedChanges();
            if (changes.Count == 0 && !outboxMessages.Any()) return;

            // Encrypt PII fields before serialization (Security requirement #4)
            var encryptedChanges = new List<object>();
            foreach (var change in changes)
            {
                if (change is FundsDepositedEvent deposited && !string.IsNullOrEmpty(deposited.Reference))
                {
                    var encrypted = await encryptionService.EncryptAsync(deposited.Reference);
                    encryptedChanges.Add(deposited with { Reference = encrypted });
                }
                else if (change is FundsWithdrawnEvent withdrawn && !string.IsNullOrEmpty(withdrawn.Reference))
                {
                    var encrypted = await encryptionService.EncryptAsync(withdrawn.Reference);
                    encryptedChanges.Add(withdrawn with { Reference = encrypted });
                }
                else
                {
                    encryptedChanges.Add(change);
                }
            }

            var expectedVersion = account.Version - changes.Count;

            if (encryptedChanges.Count > 0)
            {
                // Append events to the aggregate stream in Marten
                session.Events.Append(account.Id, encryptedChanges);

                // Crucial: Implement the Transactional Outbox (Persistence requirement #1)
                // Extract uncommitted events and save them into an OutboxMessages table in the same transaction.
                foreach (var change in encryptedChanges)
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
            // Take a snapshot every _snapshotInterval events
            if (account.Version >= (_snapshotInterval - 1) && (expectedVersion / _snapshotInterval < account.Version / _snapshotInterval))
            {
                await eventStore.SaveSnapshotAsync(account.Id, account.CreateSnapshot(), account.Version);
            }
        });

        account.MarkChangesAsCommitted();
    }
}
