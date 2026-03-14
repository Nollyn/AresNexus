using AresNexus.BuildingBlocks.Domain;

namespace AresNexus.Services.Settlement.Domain.Events;

/// <summary>
/// Event raised when an account is created.
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="Owner">The owner of the account.</param>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="SchemaVersion">The schema version of the event.</param>
/// <param name="TraceId">The trace identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
public record AccountCreatedEvent(
    Guid AccountId, 
    string Owner, 
    Guid EventId, 
    DateTime OccurredOn, 
    int SchemaVersion = 1,
    string? TraceId = null, 
    string? CorrelationId = null) : IDomainEvent;

/// <summary>
/// Event raised when funds are deposited (Version 2).
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="Money">The money deposited.</param>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="SchemaVersion">The schema version of the event.</param>
/// <param name="Reference">An optional reference for the deposit.</param>
/// <param name="TraceId">The trace identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
public record FundsDepositedEvent(
    Guid AccountId, 
    Money Money, 
    Guid EventId, 
    DateTime OccurredOn, 
    int SchemaVersion = 1,
    string? Reference = null, 
    string? TraceId = null, 
    string? CorrelationId = null) : IDomainEvent;

/// <summary>
/// Event raised when funds are withdrawn.
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="Money">The money withdrawn.</param>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="SchemaVersion">The schema version of the event.</param>
/// <param name="Reference">An optional reference for the withdrawal.</param>
/// <param name="TraceId">The trace identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
public record FundsWithdrawnEvent(
    Guid AccountId, 
    Money Money, 
    Guid EventId, 
    DateTime OccurredOn, 
    int SchemaVersion = 1,
    string? Reference = null, 
    string? TraceId = null, 
    string? CorrelationId = null) : IDomainEvent;

/// <summary>
/// Event raised when an account is locked.
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="EventId">The unique identifier of the event.</param>
/// <param name="OccurredOn">The timestamp when the event occurred.</param>
/// <param name="SchemaVersion">The schema version of the event.</param>
/// <param name="TraceId">The trace identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
public record AccountLockedEvent(
    Guid AccountId, 
    Guid EventId, 
    DateTime OccurredOn, 
    int SchemaVersion = 1,
    string? TraceId = null, 
    string? CorrelationId = null) : IDomainEvent;
