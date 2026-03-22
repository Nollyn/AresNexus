using System.Diagnostics;
using System.Diagnostics.Metrics;
using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain.Aggregates;
using AresNexus.Shared.Kernel;

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
/// <param name="meter">The OpenTelemetry meter for metrics emission.</param>
public sealed class ProcessTransactionCommandHandler(
    IAccountRepository repository,
    IEncryptionService encryptionService,
    IKeyVaultClient keyVaultClient,
    Meter meter) : IRequestHandler<ProcessTransactionCommand, bool>
{
    private readonly Counter<long> _successCounter = meter.CreateCounter<long>("settlement_success_total");
    private readonly Counter<long> _failureCounter = meter.CreateCounter<long>("settlement_failure_total");
    private readonly Counter<long> _totalCounter = meter.CreateCounter<long>("settlement_total_count_total");
    private readonly Histogram<double> _processingDuration = meter.CreateHistogram<double>("settlement_processing_seconds");

    /// <summary>
    /// Handles the transaction command.
    /// </summary>
    /// <param name="request">The transaction command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the transaction was processed successfully.</returns>
    public async Task<bool> Handle(ProcessTransactionCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Load account using the repository
            var account = await repository.GetByIdAsync(request.AccountId, cancellationToken) ?? new Account(request.AccountId, SystemConstants.SystemUser, request.TraceId, request.CorrelationId);

            // Hardened Security requirement #4: Implement Field-Level Encryption for the Reference field.
            // Use IKeyVaultClient to encrypt data before it hits the database.
            var reference = request.Reference;
            if (!string.IsNullOrEmpty(reference))
            {
                // First pass with standard encryption service
                reference = await encryptionService.EncryptAsync(reference);
                // Second pass with KeyVault for FINMA compliance
                reference = await keyVaultClient.EncryptAsync(reference, SecurityConstants.SettlementKey);
            }

            switch (request.TransactionType.ToUpperInvariant())
            {
                case TransactionTypes.Deposit:
                    account.Deposit(request.Money, reference, request.TraceId, request.CorrelationId);
                    break;
                case TransactionTypes.Withdraw:
                    account.Withdraw(request.Money, reference, request.TraceId, request.CorrelationId);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request.TransactionType), "Unknown transaction type");
            }

            // Save account via the repository
            await repository.SaveAsync(account, [], cancellationToken);

            _totalCounter.Add(1,
                new KeyValuePair<string, object?>("status", "success"),
                new KeyValuePair<string, object?>("transaction_type", request.TransactionType.ToUpperInvariant()));

            _successCounter.Add(1, 
                new KeyValuePair<string, object?>("status", "success"),
                new KeyValuePair<string, object?>("transaction_type", request.TransactionType.ToUpperInvariant()));

            return true;
        }
        catch (Exception)
        {
            _totalCounter.Add(1,
                new KeyValuePair<string, object?>("status", "failure"),
                new KeyValuePair<string, object?>("transaction_type", request.TransactionType.ToUpperInvariant()));

            _failureCounter.Add(1, 
                new KeyValuePair<string, object?>("status", "failure"),
                new KeyValuePair<string, object?>("transaction_type", request.TransactionType.ToUpperInvariant()));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _processingDuration.Record(stopwatch.Elapsed.TotalSeconds);
        }
    }
}
