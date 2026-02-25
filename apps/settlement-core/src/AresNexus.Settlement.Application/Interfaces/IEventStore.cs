using AresNexus.Shared.Kernel;

namespace AresNexus.Settlement.Application.Interfaces;

/// <summary>
/// Interface for the event store.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Saves events to the store.
    /// </summary>
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion);

    /// <summary>
    /// Loads events for an aggregate.
    /// </summary>
    Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId, int fromVersion = -1);

    /// <summary>
    /// Saves a snapshot of an aggregate.
    /// </summary>
    Task SaveSnapshotAsync<T>(Guid aggregateId, T snapshot, int version) where T : notnull;

    /// <summary>
    /// Gets the latest snapshot for an aggregate.
    /// </summary>
    Task<(T? Snapshot, int Version)> GetLatestSnapshotAsync<T>(Guid aggregateId) where T : notnull;

    /// <summary>
    /// Saves events and outbox messages atomically.
    /// </summary>
    Task SaveChangesAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion, IEnumerable<object> outboxMessages);
}
