using AresNexus.Services.Settlement.Application.Interfaces;
using AresNexus.Services.Settlement.Infrastructure.Messaging;
using AresNexus.Services.Settlement.Domain.Events;
using AresNexus.BuildingBlocks.Domain;
using Marten;
using System.Text.Json;

namespace AresNexus.Services.Settlement.Infrastructure.EventStore;

/// <summary>
/// Marten-based implementation of the event store for Swiss Tier-1 Banking.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MartenEventStore"/> class.
/// </remarks>
/// <param name="session">The Marten document session.</param>
/// <param name="upcasters">The collection of event upcasters.</param>
/// <param name="encryptionService">The PII encryption service.</param>
public sealed class MartenEventStore(IDocumentSession session, IEnumerable<IEventUpcaster> upcasters, IEncryptionService encryptionService) : IEventStore
{
    /// <inheritdoc />
    public async Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId, int fromVersion = -1)
    {
        var events = await session.Events.FetchStreamAsync(aggregateId, fromVersion: fromVersion + 1);
        var domainEvents = events.Select(e => (IDomainEvent)e.Data).ToList();

        // Decrypt PII fields (Security requirement #4)
        foreach (var @event in domainEvents)
        {
            if (@event is FundsDepositedEvent deposited)
            {
                if (!string.IsNullOrEmpty(deposited.Reference))
                {
                    // Use private reflection to update the record field (not ideal but record properties are init-only)
                    var field = typeof(FundsDepositedEvent).GetField("<Reference>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (field != null)
                    {
                        var decrypted = await encryptionService.DecryptAsync(deposited.Reference);
                        field.SetValue(deposited, decrypted);
                    }
                }
            }
            else if (@event is FundsWithdrawnEvent withdrawn)
            {
                if (!string.IsNullOrEmpty(withdrawn.Reference))
                {
                    var field = typeof(FundsWithdrawnEvent).GetField("<Reference>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (field != null)
                    {
                        var decrypted = await encryptionService.DecryptAsync(withdrawn.Reference);
                        field.SetValue(withdrawn, decrypted);
                    }
                }
            }
        }

        // Apply upcasting
        for (var i = 0; i < domainEvents.Count; i++)
        {
            var @event = domainEvents[i];
            foreach (var upcaster in upcasters)
            {
                if (upcaster.CanUpcast(@event.GetType()))
                {
                    domainEvents[i] = upcaster.Upcast(@event);
                }
            }
        }

        return domainEvents;
    }

    /// <inheritdoc />
    public Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion)
    {
        session.Events.Append(aggregateId, expectedVersion + 1, events);
        return session.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion, IEnumerable<object> outboxMessages)
    {
        // Marten's event store automatically handles versioning and transactions
        session.Events.Append(aggregateId, expectedVersion + 1, events);
        
        foreach (var msg in outboxMessages)
        {
            // We map generic outbox objects to our Infrastructure OutboxMessage entity
            session.Store(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = msg.GetType().AssemblyQualifiedName ?? msg.GetType().FullName ?? "Unknown",
                Content = JsonSerializer.Serialize(msg),
                OccurredOnUtc = DateTime.UtcNow
            });
        }

        await session.SaveChangesAsync();
    }

    /// <inheritdoc />
    public Task SaveSnapshotAsync<T>(Guid aggregateId, T snapshot, int version) where T : notnull
    {
        // Performance requirement #1: Create a Snapshot entity in the Infrastructure layer.
        var infrastructureSnapshot = new Persistence.Snapshot(
            Guid.NewGuid(),
            aggregateId,
            typeof(T).Name,
            JsonSerializer.Serialize(snapshot),
            version,
            DateTime.UtcNow);

        session.Store(infrastructureSnapshot);
        return session.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<(T? Snapshot, int Version)> GetLatestSnapshotAsync<T>(Guid aggregateId) where T : notnull
    {
        var infraSnapshot = await session.Query<Persistence.Snapshot>()
            .Where(x => x.AggregateId == aggregateId && x.AggregateType == typeof(T).Name)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync();

        if (infraSnapshot == null)
        {
            return (default, -1);
        }

        var snapshot = JsonSerializer.Deserialize<T>(infraSnapshot.Data);
        return (snapshot, infraSnapshot.Version);
    }
}
