using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain.Aggregates;
using MediatR;

namespace AresNexus.Settlement.Application.Handlers;

/// <summary>
/// Handles processing of deposit and withdrawal transactions.
/// </summary>
public sealed class ProcessTransactionCommandHandler(IEventStore eventStore, IOutboxPublisher publisher) : IRequestHandler<ProcessTransactionCommand, bool>
{
    /// <inheritdoc />
    public async Task<bool> Handle(ProcessTransactionCommand request, CancellationToken cancellationToken)
    {
        var history = await eventStore.GetEventsAsync(request.AccountId);
        var account = new Account();
        account.LoadsFromHistory(history);

        if (history.Count == 0)
        {
            _ = new Account(request.AccountId, "SYSTEM"); // initial create if new
            account = new Account(request.AccountId, "SYSTEM");
        }

        switch (request.TransactionType.ToUpperInvariant())
        {
            case "DEPOSIT":
                account.Deposit(request.Amount);
                break;
            case "WITHDRAW":
                account.Withdraw(request.Amount);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.TransactionType), "Unknown transaction type");
        }

        var changes = account.GetUncommittedChanges();
        await eventStore.SaveEventsAsync(account.Id, changes, account.Version);
        account.MarkChangesAsCommitted();

        // publish a lightweight integration event
        await publisher.PublishAsync("settlements.transactions", new
        {
            account.Id,
            request.Amount,
            request.TransactionType
        });

        return true;
    }
}
