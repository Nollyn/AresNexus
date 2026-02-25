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
    Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId);
}
