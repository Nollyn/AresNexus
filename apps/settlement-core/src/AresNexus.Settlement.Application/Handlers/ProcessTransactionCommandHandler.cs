using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain.Aggregates;
using MediatR;

namespace AresNexus.Settlement.Application.Handlers;

/// <summary>
/// Handles processing of deposit and withdrawal transactions.
/// </summary>
public sealed class ProcessTransactionCommandHandler(IEventStore eventStore, IIdempotencyStore idempotencyStore, IEncryptionService encryptionService) : IRequestHandler<ProcessTransactionCommand, bool>
{
    /// <inheritdoc />
    public async Task<bool> Handle(ProcessTransactionCommand request, CancellationToken cancellationToken)
    {
        // Check idempotency
        if (await idempotencyStore.ExistsAsync(request.IdempotencyKey))
        {
            return await idempotencyStore.GetAsync<bool>(request.IdempotencyKey);
        }

        // Load from snapshot if available
        var (snapshot, snapshotVersion) = await eventStore.GetLatestSnapshotAsync<Account.Snapshot>(request.AccountId);
        var account = new Account();
        
        if (snapshot != null)
        {
            account.LoadFromSnapshot(snapshot);
        }

        var history = await eventStore.GetEventsAsync(request.AccountId, snapshotVersion);
        account.LoadsFromHistory(history);

        if (snapshot == null && history.Count == 0)
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

        var changes = account.GetUncommittedChanges();
        
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

        await eventStore.SaveChangesAsync(account.Id, changes, account.Version - changes.Count, outboxMessages);

        // Snapshotting logic: Every 50 events
        if (account.Version >= 50 && (account.Version - changes.Count) / 50 < account.Version / 50)
        {
            await eventStore.SaveSnapshotAsync(account.Id, account.CreateSnapshot(), account.Version);
        }

        account.MarkChangesAsCommitted();

        await idempotencyStore.StoreAsync(request.IdempotencyKey, true);

        return true;
    }
}
