using System.Collections.Concurrent;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Infrastructure.Messaging;
using AresNexus.Shared.Kernel;
using Newtonsoft.Json;

namespace AresNexus.Settlement.Infrastructure.EventStore;

/// <summary>
/// Mock implementation of an EventStore using an in-memory structure inspired by CosmosDB patterns.
/// </summary>
public sealed class InMemoryCosmosEventStore : IEventStore
{
    private readonly ConcurrentDictionary<Guid, List<IDomainEvent>> _streams = new();
    private readonly ConcurrentDictionary<Guid, (object Snapshot, int Version)> _snapshots = new();
    private readonly ConcurrentQueue<OutboxMessage> _outbox = new();

    /// <inheritdoc />
    public Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId, int fromVersion = -1)
    {
        var list = _streams.TryGetValue(aggregateId, out var events) ? events : [];

        return Task.FromResult(fromVersion != -1 ? list.Skip(fromVersion + 1).ToList() : [.. list]);
    }

    /// <inheritdoc />
    public Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion)
    {
        return SaveChangesAsync(aggregateId, events, expectedVersion, []);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion, IEnumerable<object> outboxMessages)
    {
        var stream = _streams.GetOrAdd(aggregateId, _ => []);
        var currentVersion = stream.Count - 1;
        if (currentVersion != expectedVersion && expectedVersion != -1)
        {
            throw new InvalidOperationException("Concurrency conflict detected while saving events.");
        }

        // Atomically (simulated) add events and outbox messages
        stream.AddRange(events);
        
        foreach (var msg in outboxMessages)
        {
            _outbox.Enqueue(new OutboxMessage
            {
                Type = msg.GetType().FullName ?? "Unknown",
                Content = JsonConvert.SerializeObject(msg),
                CreatedOnUtc = DateTime.UtcNow
            });
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveSnapshotAsync<T>(Guid aggregateId, T snapshot, int version)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        _snapshots[aggregateId] = (snapshot, version);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<(T? Snapshot, int Version)> GetLatestSnapshotAsync<T>(Guid aggregateId)
    {
        return _snapshots.TryGetValue(aggregateId, out var result) ? Task.FromResult(((T?)result.Snapshot, result.Version)) : Task.FromResult<(T?, int)>((default, -1));
    }

    /// <summary>
    /// Gets pending outbox messages (for the processor).
    /// </summary>
    /// <returns>A list of unprocessed <see cref="OutboxMessage"/>.</returns>
    public List<OutboxMessage> GetUnprocessedOutboxMessages()
    {
        return [.. _outbox.Where(m => m.ProcessedOnUtc == null)];
    }
}
