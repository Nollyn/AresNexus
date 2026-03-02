using AresNexus.Shared.Kernel;

namespace AresNexus.Settlement.Infrastructure.EventStore;

/// <summary>
/// Base class for event upcasters to handle version evolution of domain events.
/// </summary>
public abstract class EventUpcaster : IEventUpcaster
{
    /// <inheritdoc />
    public abstract bool CanUpcast(Type eventType);

    /// <inheritdoc />
    public abstract IDomainEvent Upcast(IDomainEvent @event);
}

/// <summary>
/// Upcasts FundsDepositedEvent from V1 (no currency) to V2 (with currency).
/// </summary>
public sealed class MoneyDeposited_v1_to_v2_Upcaster : EventUpcaster
{
    /// <inheritdoc />
    public override bool CanUpcast(Type eventType)
    {
        return eventType == typeof(FundsDepositedEvent_v1);
    }

    /// <inheritdoc />
    public override IDomainEvent Upcast(IDomainEvent @event)
    {
        if (@event is FundsDepositedEvent_v1 v1)
        {
            return new FundsDepositedEvent(v1.AccountId, new Money(v1.Amount, "CHF"), v1.EventId, v1.OccurredOn, v1.Reference, v1.TraceId, v1.CorrelationId);
        }
        return @event;
    }
}

/// <summary>
/// Represents the legacy version of the FundsDepositedEvent.
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="Amount">The amount deposited.</param>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="Reference">An optional reference for the deposit.</param>
/// <param name="TraceId">The trace identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
public record FundsDepositedEvent_v1(
    Guid AccountId, 
    decimal Amount, 
    Guid EventId, 
    DateTime OccurredOn, 
    string? Reference = null,
    string? TraceId = null,
    string? CorrelationId = null) : IDomainEvent;
