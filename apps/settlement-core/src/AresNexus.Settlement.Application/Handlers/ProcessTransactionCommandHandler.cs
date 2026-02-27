using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain.Aggregates;
using MediatR;

namespace AresNexus.Settlement.Application.Handlers;

/// <summary>
/// Handles processing of deposit and withdrawal transactions with PII encryption.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProcessTransactionCommandHandler"/> class.
/// </remarks>
/// <param name="repository">The account repository.</param>
/// <param name="encryptionService">The PII encryption service for Zurich compliance.</param>
public sealed class ProcessTransactionCommandHandler(IAccountRepository repository, IEncryptionService encryptionService) : IRequestHandler<ProcessTransactionCommand, bool>
{
    /// <summary>
    /// Handles the transaction command.
    /// </summary>
    /// <param name="request">The transaction command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if the transaction was processed successfully.</returns>
    public async Task<bool> Handle(ProcessTransactionCommand request, CancellationToken cancellationToken)
    {
        // Load account using the repository
        var account = await repository.GetByIdAsync(request.AccountId, cancellationToken);
        
        if (account == null)
        {
            account = new Account(request.AccountId, "SYSTEM");
        }

        // Encrypt reference if present (Security requirement #5)
        // Ensures Reference in MoneyDeposited/MoneyWithdrawn events is encrypted before they hit the database.
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

        // Save account via the repository
        // The repository will automatically extract uncommitted events and save them to the outbox.
        await repository.SaveAsync(account, [], cancellationToken);

        return true;
    }
}
