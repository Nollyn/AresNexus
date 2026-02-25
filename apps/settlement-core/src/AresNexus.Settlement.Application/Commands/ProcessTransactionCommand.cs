using MediatR;

namespace AresNexus.Settlement.Application.Commands;

/// <summary>
/// Command to process a transaction.
/// </summary>
public record ProcessTransactionCommand(Guid AccountId, decimal Amount, string TransactionType) : IRequest<bool>;
