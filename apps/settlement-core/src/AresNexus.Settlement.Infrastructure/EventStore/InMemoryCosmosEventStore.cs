using System.Collections.Concurrent;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Shared.Kernel;

namespace AresNexus.Settlement.Infrastructure.EventStore;

/// <summary>
/// Mock implementation of an EventStore using an in-memory structure inspired by CosmosDB patterns.
/// </summary>
public sealed class InMemoryCosmosEventStore : IEventStore
{
    private readonly ConcurrentDictionary<Guid, List<IDomainEvent>> _streams = new();

    /// <inheritdoc />
    public Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId)
    {
        var list = _streams.TryGetValue(aggregateId, out var events) ? events : new List<IDomainEvent>();
        // Return a copy to prevent external mutation
        return Task.FromResult(list.ToList());
    }

    /// <inheritdoc />
    public Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion)
    {
        var stream = _streams.GetOrAdd(aggregateId, _ => new List<IDomainEvent>());
        // Simple optimistic concurrency simulation based on count - 1 equals expectedVersion
        var currentVersion = stream.Count - 1;
        if (currentVersion != expectedVersion && expectedVersion != -1)
        {
            throw new InvalidOperationException("Concurrency conflict detected while saving events.");
        }

        stream.AddRange(events);
        return Task.CompletedTask;
    }
}
