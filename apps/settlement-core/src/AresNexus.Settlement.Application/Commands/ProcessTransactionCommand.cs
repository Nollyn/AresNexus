using AresNexus.Settlement.Domain;
using MediatR;

namespace AresNexus.Settlement.Application.Commands;

/// <summary>
/// Command to process a transaction.
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="Money">The money to process.</param>
/// <param name="TransactionType">The type of transaction (e.g., DEPOSIT, WITHDRAW).</param>
/// <param name="IdempotencyKey">A unique key to ensure the request is only processed once.</param>
/// <param name="Reference">An optional reference or description for the transaction.</param>
/// <param name="TraceId">The trace identifier.</param>
/// <param name="CorrelationId">The correlation identifier.</param>
public record ProcessTransactionCommand(
    Guid AccountId, 
    Money Money, 
    string TransactionType, 
    Guid IdempotencyKey,
    string? Reference = null,
    string? TraceId = null,
    string? CorrelationId = null) : IRequest<bool>;
