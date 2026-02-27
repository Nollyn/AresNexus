using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain.Events;
using AresNexus.Shared.Kernel;

namespace AresNexus.Settlement.Infrastructure.EventStore;

/// <summary>
/// Upcasts FundsDepositedEvent from V1 (no currency) to V2 (with currency).
/// </summary>
public sealed class MoneyDeposited_v1_to_v2_Upcaster : IEventUpcaster
{
    /// <inheritdoc />
    public bool CanUpcast(Type eventType)
    {
        return eventType == typeof(FundsDepositedEvent_v1);
    }

    /// <inheritdoc />
    public IDomainEvent Upcast(IDomainEvent @event)
    {
        if (@event is FundsDepositedEvent_v1 v1)
        {
            return new FundsDepositedEvent(v1.AccountId, v1.Amount, "CHF", v1.EventId, v1.OccurredOn, v1.Reference);
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
