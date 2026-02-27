using AresNexus.Shared.Kernel;

namespace AresNexus.Settlement.Application.Interfaces;

/// <summary>
/// Defines an interface for event upcasters to handle version evolution of domain events.
/// </summary>
public interface IEventUpcaster
{
    /// <summary>
    /// Checks if this upcaster can handle the specified event type.
    /// </summary>
    /// <param name="eventType">The type of the event.</param>
    /// <returns>True if it can upcast; otherwise, false.</returns>
    bool CanUpcast(Type eventType);

    /// <summary>
    /// Upcasts the domain event to a newer version.
    /// </summary>
    /// <param name="event">The original domain event.</param>
    /// <returns>The upcasted domain event.</returns>
    IDomainEvent Upcast(IDomainEvent @event);
}
