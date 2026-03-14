using AresNexus.Services.Settlement.Application.Commands;
using AresNexus.Services.Settlement.Application.Interfaces;
using AresNexus.Services.Settlement.Domain.Aggregates;
using AresNexus.BuildingBlocks.Domain;
using MediatR;

namespace AresNexus.Services.Settlement.Application.Handlers;

/// <summary>
/// Handles processing of deposit and withdrawal transactions with PII encryption.
/// </summary>
public sealed class ProcessTransactionCommandHandler : IRequestHandler<ProcessTransactionCommand, bool>
{
    private readonly IAccountRepository _repository;
    private readonly IEncryptionService _encryptionService;
    private readonly IKeyVaultClient _keyVaultClient;
    private readonly System.Diagnostics.Metrics.Counter<long> _successCounter;
    private readonly System.Diagnostics.Metrics.Counter<long> _failureCounter;
    private readonly System.Diagnostics.Metrics.Counter<long> _totalCounter;
    private readonly System.Diagnostics.Metrics.Histogram<double> _processingDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessTransactionCommandHandler"/> class.
    /// </summary>
    /// <param name="repository">The account repository.</param>
    /// <param name="encryptionService">The PII encryption service for Zurich compliance.</param>
    /// <param name="keyVaultClient">The mock KeyVault client for FINMA/DORA compliance.</param>
    /// <param name="meter">The OpenTelemetry meter for metrics emission.</param>
    public ProcessTransactionCommandHandler(
        IAccountRepository repository, 
        IEncryptionService encryptionService,
        IKeyVaultClient keyVaultClient,
        System.Diagnostics.Metrics.Meter meter)
    {
        _repository = repository;
        _encryptionService = encryptionService;
        _keyVaultClient = keyVaultClient;
        _successCounter = meter.CreateCounter<long>("settlement_success_total");
        _failureCounter = meter.CreateCounter<long>("settlement_failure_total");
        _totalCounter = meter.CreateCounter<long>("settlement_total_count_total");
        _processingDuration = meter.CreateHistogram<double>("settlement_processing_seconds");
    }

    /// <summary>
    /// Handles the transaction command.
    /// </summary>
    /// <param name="request">The transaction command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains true if the transaction was processed successfully.</returns>
    public async Task<bool> Handle(ProcessTransactionCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            // Load account using the repository
            var account = await _repository.GetByIdAsync(request.AccountId, cancellationToken);
            
            if (account == null)
            {
                account = new Account(request.AccountId, SystemConstants.SystemUser, request.TraceId, request.CorrelationId);
            }

            // Hardened Security requirement #4: Implement Field-Level Encryption for the Reference field.
            // Use IKeyVaultClient to encrypt data before it hits the database.
            var reference = request.Reference;
            if (!string.IsNullOrEmpty(reference))
            {
                // First pass with standard encryption service
                reference = await _encryptionService.EncryptAsync(reference);
                // Second pass with KeyVault for FINMA compliance
                reference = await _keyVaultClient.EncryptAsync(reference, SecurityConstants.SettlementKey);
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
            await _repository.SaveAsync(account, [], cancellationToken);

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
