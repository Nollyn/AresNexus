namespace AresNexus.Shared.Kernel;

/// <summary>
/// Represents a domain event.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of the event.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the timestamp when the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }

    /// <summary>
    /// Gets the Trace ID for distributed tracing (FINMA requirement).
    /// </summary>
    string? TraceId { get; init; }

    /// <summary>
    /// Gets the Correlation ID for distributed tracing (DORA requirement).
    /// </summary>
    string? CorrelationId { get; init; }
}

/// <summary>
/// Base class for aggregate roots with event sourcing support.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _changes = [];

    /// <summary>
    /// Gets the unique identifier of the aggregate.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets the current version of the aggregate.
    /// </summary>
    public int Version { get; protected set; } = -1;

    /// <summary>
    /// Gets the uncommitted changes for this aggregate.
    /// </summary>
    /// <returns>A read-only collection of domain events.</returns>
    public IReadOnlyCollection<IDomainEvent> GetUncommittedChanges() => _changes.AsReadOnly();

    /// <summary>
    /// Clears the uncommitted changes.
    /// </summary>
    public void MarkChangesAsCommitted() => _changes.Clear();

    /// <summary>
    /// Loads the aggregate state from a history of events.
    /// </summary>
    /// <param name="history">The collection of domain events to apply.</param>
    public void LoadsFromHistory(IEnumerable<IDomainEvent> history)
    {
        // Resiliency requirement #1: Update the Account loading logic to:
        // Find the latest Snapshot (done in Repository)
        // Replay only the events occurred after that snapshot.
        foreach (var e in history)
        {
            ApplyChange(e, false);
        }
    }

    /// <summary>
    /// Applies a new change to the aggregate.
    /// </summary>
    /// <param name="event">The domain event to apply.</param>
    protected void ApplyChange(IDomainEvent @event) => ApplyChange(@event, true);

    private void ApplyChange(IDomainEvent @event, bool isNew)
    {
        var method = GetType().GetMethod("Apply", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, [@event.GetType()], null);
        if (method != null)
        {
            method.Invoke(this, [@event]);
        }
        else
        {
            throw new InvalidOperationException($"Method 'Apply' not found for event type '{@event.GetType().Name}' on aggregate '{this.GetType().Name}'.");
        }

        if (isNew)
        {
            _changes.Add(@event);
        }

        Version++;
    }
}

/// <summary>
/// Helper for dynamic dispatch.
/// </summary>
public static class PrivateReflectionDynamicObjectExtensions
{
    /// <summary>
    /// Converts an object to a dynamic object for accessing private members.
    /// </summary>
    public static dynamic AsDynamic(this object o) => o;
}
