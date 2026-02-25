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
    public decimal Balance { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Account"/> class.
    /// </summary>
    public Account() { }

    /// <summary>
    /// Creates a new account.
    /// </summary>
    public Account(Guid id, string owner)
    {
        ApplyChange(new AccountCreatedEvent(id, owner, Guid.NewGuid(), DateTime.UtcNow));
    }

    /// <summary>
    /// Deposits funds into the account.
    /// </summary>
    public void Deposit(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
        ApplyChange(new FundsDepositedEvent(Id, amount, Guid.NewGuid(), DateTime.UtcNow));
    }

    /// <summary>
    /// Withdraws funds from the account.
    /// </summary>
    public void Withdraw(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be positive", nameof(amount));
        if (Balance < amount) throw new InvalidOperationException("Insufficient funds");
        ApplyChange(new FundsWithdrawnEvent(Id, amount, Guid.NewGuid(), DateTime.UtcNow));
    }

    /// <summary>
    /// Applies the <see cref="AccountCreatedEvent"/>.
    /// </summary>
    public void Apply(AccountCreatedEvent e)
    {
        Id = e.AccountId;
        Owner = e.Owner;
        Balance = 0;
    }

    /// <summary>
    /// Applies the <see cref="FundsDepositedEvent"/>.
    /// </summary>
    public void Apply(FundsDepositedEvent e)
    {
        Balance += e.Amount;
    }

    /// <summary>
    /// Applies the <see cref="FundsWithdrawnEvent"/>.
    /// </summary>
    public void Apply(FundsWithdrawnEvent e)
    {
        Balance -= e.Amount;
    }
}
