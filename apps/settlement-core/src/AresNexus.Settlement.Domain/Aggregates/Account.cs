using AresNexus.Settlement.Domain.Events;
using AresNexus.Shared.Kernel;

namespace AresNexus.Settlement.Domain.Aggregates;

/// <summary>
/// Aggregate root for a settlement account.
/// </summary>
public sealed class Account : AggregateRoot
{
    /// <summary>
    /// Gets the account owner.
    /// </summary>
    public string Owner { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the current balance.
    /// </summary>
    public Money Balance { get; private set; } = new(0);

    /// <summary>
    /// Initializes a new instance of the <see cref="Account"/> class.
    /// </summary>
    public Account() { }

    /// <summary>
    /// Creates a new account.
    /// </summary>
    public Account(Guid id, string owner, string? traceId = null, string? correlationId = null)
    {
        ApplyChange(new AccountCreatedEvent(id, owner, Guid.NewGuid(), DateTime.UtcNow, traceId, correlationId));
    }

    /// <summary>
    /// Deposits funds into the account.
    /// </summary>
    /// <param name="money">The money to deposit.</param>
    /// <param name="reference">The optional reference.</param>
    /// <param name="traceId">The trace identifier.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    public void Deposit(Money money, string? reference = null, string? traceId = null, string? correlationId = null)
    {
        ApplyChange(new FundsDepositedEvent(Id, money, Guid.NewGuid(), DateTime.UtcNow, reference, traceId, correlationId));
    }

    /// <summary>
    /// Withdraws funds from the account.
    /// </summary>
    public void Withdraw(Money money, string? reference = null, string? traceId = null, string? correlationId = null)
    {
        if (Balance.Amount < money.Amount) throw new InvalidOperationException("Insufficient funds");
        ApplyChange(new FundsWithdrawnEvent(Id, money, Guid.NewGuid(), DateTime.UtcNow, reference, traceId, correlationId));
    }

    /// <summary>
    /// Represents a snapshot of the account state.
    /// </summary>
    public record Snapshot(Guid Id, string Owner, Money Balance, int Version);

    /// <summary>
    /// Creates a snapshot of the current state.
    /// </summary>
    public Snapshot CreateSnapshot() => new(Id, Owner, Balance, Version);

    /// <summary>
    /// Loads the aggregate state from a snapshot.
    /// </summary>
    public void LoadFromSnapshot(Snapshot snapshot)
    {
        Id = snapshot.Id;
        Owner = snapshot.Owner;
        Balance = snapshot.Balance;
        Version = snapshot.Version;
    }

    /// <summary>
    /// Applies the <see cref="AccountCreatedEvent"/>.
    /// </summary>
    public void Apply(AccountCreatedEvent e)
    {
        Id = e.AccountId;
        Owner = e.Owner;
        Balance = new Money(0);
    }

    /// <summary>
    /// Applies the <see cref="FundsDepositedEvent"/>.
    /// </summary>
    public void Apply(FundsDepositedEvent e)
    {
        Balance += e.Money;
    }

    /// <summary>
    /// Applies the <see cref="FundsWithdrawnEvent"/>.
    /// </summary>
    public void Apply(FundsWithdrawnEvent e)
    {
        Balance -= e.Money;
    }
}
