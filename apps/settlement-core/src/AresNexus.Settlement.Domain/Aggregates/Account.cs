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
    /// Gets a value indicating whether the account is locked.
    /// </summary>
    public bool IsLocked { get; private set; }

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
        if (IsLocked) throw new AccountLockedException(Id);
        ApplyChange(new FundsDepositedEvent(Id, money, Guid.NewGuid(), DateTime.UtcNow, reference, traceId, correlationId));
    }

    /// <summary>
    /// Withdraws funds from the account.
    /// </summary>
    /// <param name="money">The money to withdraw.</param>
    /// <param name="reference">The optional reference.</param>
    /// <param name="traceId">The trace identifier.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    public void Withdraw(Money money, string? reference = null, string? traceId = null, string? correlationId = null)
    {
        if (IsLocked) throw new AccountLockedException(Id);
        if (Balance.Amount < money.Amount) throw new InvalidOperationException("Insufficient funds");
        ApplyChange(new FundsWithdrawnEvent(Id, money, Guid.NewGuid(), DateTime.UtcNow, reference, traceId, correlationId));
    }

    /// <summary>
    /// Locks the account.
    /// </summary>
    /// <param name="traceId">The trace identifier.</param>
    /// <param name="correlationId">The correlation identifier.</param>
    public void Lock(string? traceId = null, string? correlationId = null)
    {
        if (IsLocked) return;
        ApplyChange(new AccountLockedEvent(Id, Guid.NewGuid(), DateTime.UtcNow, traceId, correlationId));
    }

    /// <summary>
    /// Represents a snapshot of the account state.
    /// </summary>
    public record Snapshot(Guid Id, string Owner, Money Balance, bool IsLocked, int Version);

    /// <summary>
    /// Creates a snapshot of the current state.
    /// </summary>
    public Snapshot CreateSnapshot() => new(Id, Owner, Balance, IsLocked, Version);

    /// <summary>
    /// Loads the aggregate state from a snapshot.
    /// </summary>
    public void LoadFromSnapshot(Snapshot snapshot)
    {
        if (Version > 0 && Version != snapshot.Version)
        {
            throw new InvalidOperationException($"Cannot load snapshot with version {snapshot.Version} because current aggregate version is {Version}");
        }

        Id = snapshot.Id;
        Owner = snapshot.Owner;
        Balance = snapshot.Balance;
        IsLocked = snapshot.IsLocked;
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

    /// <summary>
    /// Applies the <see cref="AccountLockedEvent"/>.
    /// </summary>
    public void Apply(AccountLockedEvent e)
    {
        IsLocked = true;
    }
}
