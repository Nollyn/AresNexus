using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Infrastructure.Messaging;
using AresNexus.Shared.Kernel;
using Marten;
using System.Text.Json;

namespace AresNexus.Settlement.Infrastructure.EventStore;

/// <summary>
/// Marten-based implementation of the event store for Swiss Tier-1 Banking.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MartenEventStore"/> class.
/// </remarks>
/// <param name="session">The Marten document session.</param>
public sealed class MartenEventStore(IDocumentSession session) : IEventStore
{
    /// <summary>
    /// Fetches events for a given aggregate starting from a specific version.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="fromVersion">The version to start fetching from (exclusive).</param>
    /// <returns>A list of domain events.</returns>
    public async Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId, int fromVersion = -1)
    {
        var events = await session.Events.FetchStreamAsync(aggregateId, fromVersion: fromVersion + 1);
        return [.. events.Select(e => (IDomainEvent)e.Data)];
    }

    /// <summary>
    /// Appends events to the aggregate stream and saves changes.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="events">The collection of domain events to append.</param>
    /// <param name="expectedVersion">The expected version of the aggregate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion)
    {
        session.Events.Append(aggregateId, expectedVersion + 1, events);
        return session.SaveChangesAsync();
    }

    /// <summary>
    /// Saves events and outbox messages in a single transaction.
    /// </summary>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="events">The collection of domain events.</param>
    /// <param name="expectedVersion">The expected version of the aggregate.</param>
    /// <param name="outboxMessages">The collection of outbox messages.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Saves a snapshot of the aggregate state.
    /// </summary>
    /// <typeparam name="T">The type of the snapshot.</typeparam>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <param name="snapshot">The snapshot data.</param>
    /// <param name="version">The version at which the snapshot was taken.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task SaveSnapshotAsync<T>(Guid aggregateId, T snapshot, int version) where T : notnull
    {
        // Marten stores snapshots as documents
        session.Store(snapshot);
        return session.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieves the latest snapshot for a given aggregate.
    /// </summary>
    /// <typeparam name="T">The type of the snapshot.</typeparam>
    /// <param name="aggregateId">The unique identifier of the aggregate.</param>
    /// <returns>The snapshot and its version, or null if not found.</returns>
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
