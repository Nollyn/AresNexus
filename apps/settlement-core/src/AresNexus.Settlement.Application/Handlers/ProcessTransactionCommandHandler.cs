using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain.Aggregates;
using MediatR;

namespace AresNexus.Settlement.Application.Handlers;

/// <summary>
/// Handles processing of deposit and withdrawal transactions.
/// </summary>
public sealed class ProcessTransactionCommandHandler(IAccountRepository repository, IIdempotencyStore idempotencyStore, IEncryptionService encryptionService) : IRequestHandler<ProcessTransactionCommand, bool>
{
    /// <inheritdoc />
    public async Task<bool> Handle(ProcessTransactionCommand request, CancellationToken cancellationToken)
    {
        // Check idempotency
        if (await idempotencyStore.ExistsAsync(request.IdempotencyKey))
        {
            return await idempotencyStore.GetAsync<bool>(request.IdempotencyKey);
        }

        // Load account using the repository
        var account = await repository.GetByIdAsync(request.AccountId, cancellationToken);
        
        if (account == null)
        {
            account = new Account(request.AccountId, "SYSTEM");
        }

        // Encrypt reference if present
        var reference = request.Reference;
        if (!string.IsNullOrEmpty(reference))
        {
            reference = await encryptionService.EncryptAsync(reference);
        }

        switch (request.TransactionType.ToUpperInvariant())
        {
            case "DEPOSIT":
                account.Deposit(request.Amount, reference);
                break;
            case "WITHDRAW":
                account.Withdraw(request.Amount, reference);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.TransactionType), "Unknown transaction type");
        }

        // Define outbox message
        var outboxMessages = new List<object>
        {
            new
            {
                account.Id,
                request.Amount,
                request.TransactionType,
                Reference = reference
            }
        };

        // Save account and outbox messages atomically via the repository
        await repository.SaveAsync(account, outboxMessages, cancellationToken);

        await idempotencyStore.StoreAsync(request.IdempotencyKey, true);

        return true;
    }
}
