using AresNexus.Shared.Kernel;

namespace AresNexus.Settlement.Domain.Events;

/// <summary>
/// Event raised when an account is created.
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="Owner">The owner of the account.</param>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
public record AccountCreatedEvent(Guid AccountId, string Owner, Guid EventId, DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Event raised when funds are deposited.
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="Amount">The amount deposited.</param>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="Reference">An optional reference for the deposit.</param>
public record FundsDepositedEvent(Guid AccountId, decimal Amount, Guid EventId, DateTime OccurredOn, string? Reference = null) : IDomainEvent;

/// <summary>
/// Event raised when funds are withdrawn.
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="Amount">The amount withdrawn.</param>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="Reference">An optional reference for the withdrawal.</param>
public record FundsWithdrawnEvent(Guid AccountId, decimal Amount, Guid EventId, DateTime OccurredOn, string? Reference = null) : IDomainEvent;
