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
/// <param name="keyVaultClient">The mock KeyVault client for FINMA/DORA compliance.</param>
public sealed class ProcessTransactionCommandHandler(
    IAccountRepository repository, 
    IEncryptionService encryptionService,
    IKeyVaultClient keyVaultClient) : IRequestHandler<ProcessTransactionCommand, bool>
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
            account = new Account(request.AccountId, "SYSTEM", request.TraceId, request.CorrelationId);
        }

        // Hardened Security requirement #4: Implement Field-Level Encryption for the Reference field.
        // Use IKeyVaultClient to encrypt data before it hits the database.
        var reference = request.Reference;
        if (!string.IsNullOrEmpty(reference))
        {
            // First pass with standard encryption service
            reference = await encryptionService.EncryptAsync(reference);
            // Second pass with KeyVault for FINMA compliance
            reference = await keyVaultClient.EncryptAsync(reference, "AresNexus-Settle-Key");
        }

        switch (request.TransactionType.ToUpperInvariant())
        {
            case "DEPOSIT":
                account.Deposit(request.Amount, "CHF", reference, request.TraceId, request.CorrelationId);
                break;
            case "WITHDRAW":
                account.Withdraw(request.Amount, reference, request.TraceId, request.CorrelationId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.TransactionType), "Unknown transaction type");
        }

        // Save account via the repository
        await repository.SaveAsync(account, [], cancellationToken);

        return true;
    }
}
