using AresNexus.Shared.Kernel;

namespace AresNexus.Settlement.Domain.Events;

/// <summary>
/// Event raised when an account is created.
/// </summary>
public record AccountCreatedEvent(Guid AccountId, string Owner, Guid EventId, DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Event raised when funds are deposited.
/// </summary>
public record FundsDepositedEvent(Guid AccountId, decimal Amount, Guid EventId, DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Event raised when funds are withdrawn.
/// </summary>
public record FundsWithdrawnEvent(Guid AccountId, decimal Amount, Guid EventId, DateTime OccurredOn) : IDomainEvent;
