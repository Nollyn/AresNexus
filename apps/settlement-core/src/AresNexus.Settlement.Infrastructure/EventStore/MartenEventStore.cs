using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Infrastructure.Messaging;
using AresNexus.Shared.Kernel;
using Marten;

namespace AresNexus.Settlement.Infrastructure.EventStore;

/// <summary>
/// Marten-based implementation of the event store.
/// </summary>
public sealed class MartenEventStore(IDocumentSession session) : IEventStore
{
    /// <inheritdoc />
    public async Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId, int fromVersion = -1)
    {
        var events = await session.Events.FetchStreamAsync(aggregateId, fromVersion: fromVersion + 1);
        return [.. events.Select(e => (IDomainEvent)e.Data)];
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
            // In a production scenario, we might want to use Marten's native Outbox feature,
            // but here we follow the explicit request to save into an OutboxMessages table.
            session.Store(new OutboxMessage
            {
                Type = msg.GetType().FullName ?? "Unknown",
                Content = Newtonsoft.Json.JsonConvert.SerializeObject(msg),
                OccurredOnUtc = DateTime.UtcNow
            });
        }

        await session.SaveChangesAsync();
    }

    /// <inheritdoc />
    public Task SaveSnapshotAsync<T>(Guid aggregateId, T snapshot, int version) where T : notnull
    {
        // Marten can store snapshots as documents
        session.Store(snapshot);
        return session.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<(T? Snapshot, int Version)> GetLatestSnapshotAsync<T>(Guid aggregateId) where T : notnull
    {
        var snapshot = await session.LoadAsync<T>(aggregateId);
        // We assume the snapshot object has a Version property as per our Account.Snapshot record
        var version = -1;
        if (snapshot == null) return (snapshot, version);
        var prop = typeof(T).GetProperty("Version");
        if (prop != null)
        {
            version = (int)(prop.GetValue(snapshot) ?? -1);
        }
        return (snapshot, version);
    }
}
