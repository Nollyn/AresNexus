using MediatR;

namespace AresNexus.Settlement.Application.Commands;

/// <summary>
/// Command to process a transaction.
/// </summary>
/// <param name="AccountId">The unique identifier of the account.</param>
/// <param name="Amount">The amount to process.</param>
/// <param name="TransactionType">The type of transaction (e.g., DEPOSIT, WITHDRAW).</param>
/// <param name="IdempotencyKey">A unique key to ensure the request is only processed once.</param>
/// <param name="Reference">An optional reference or description for the transaction.</param>
public record ProcessTransactionCommand(
    Guid AccountId, 
    decimal Amount, 
    string TransactionType, 
    Guid IdempotencyKey,
    string? Reference = null) : IRequest<bool>;
