using AresNexus.Shared.Kernel;

namespace AresNexus.Settlement.Domain.Events;

/// <summary>
/// Event raised when an account is created.
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="Owner">The owner of the account.</param>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="TraceId">The trace identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
public record AccountCreatedEvent(
    Guid AccountId, 
    string Owner, 
    Guid EventId, 
    DateTime OccurredOn, 
    string? TraceId = null, 
    string? CorrelationId = null) : IDomainEvent;

/// <summary>
/// Event raised when funds are deposited (Version 2).
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="Amount">The amount deposited.</param>
/// <param name="Currency">The currency of the deposit (added in V2).</param>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="Reference">An optional reference for the deposit.</param>
/// <param name="TraceId">The trace identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
public record FundsDepositedEvent(
    Guid AccountId, 
    decimal Amount, 
    string Currency, 
    Guid EventId, 
    DateTime OccurredOn, 
    string? Reference = null, 
    string? TraceId = null, 
    string? CorrelationId = null) : IDomainEvent;

/// <summary>
/// Event raised when funds are withdrawn.
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="Amount">The amount withdrawn.</param>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="Reference">An optional reference for the withdrawal.</param>
/// <param name="TraceId">The trace identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
public record FundsWithdrawnEvent(
    Guid AccountId, 
    decimal Amount, 
    Guid EventId, 
    DateTime OccurredOn, 
    string? Reference = null, 
    string? TraceId = null, 
    string? CorrelationId = null) : IDomainEvent;
