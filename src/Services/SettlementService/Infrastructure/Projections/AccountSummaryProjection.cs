using Marten.Events.Aggregation;
using AresNexus.Services.Settlement.Domain.Events;

namespace AresNexus.Services.Settlement.Infrastructure.Projections;

/// <summary>
/// Explicit Read-Model for Account Summary.
/// </summary>
public record AccountSummary
{
    /// <summary>Gets or sets the account identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the account owner.</summary>
    public string Owner { get; set; } = string.Empty;
    /// <summary>Gets or sets the current balance.</summary>
    public decimal Balance { get; set; }
    /// <summary>Gets or sets the number of transactions.</summary>
    public int TransactionCount { get; set; }
    /// <summary>Gets or sets the last updated timestamp.</summary>
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Explicit Read-Model projection for Account Summary.
/// Demonstrates CQRS maturity by separating the read-model from the write-model.
/// </summary>
public class AccountSummaryProjection : SingleStreamProjection<AccountSummary, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountSummaryProjection"/> class.
    /// </summary>
    public AccountSummaryProjection()
    {
        // Define how to handle events
    }

    /// <summary>
    /// Applies the <see cref="AccountCreatedEvent"/> to the summary.
    /// </summary>
    /// <param name="event">The account created event.</param>
    /// <param name="view">The account summary view.</param>
    public void Apply(AccountCreatedEvent @event, AccountSummary view)
    {
        view.Id = @event.AccountId;
        view.Owner = @event.Owner;
        view.Balance = 0;
        view.TransactionCount = 0;
        view.LastUpdated = @event.OccurredOn;
    }

    /// <summary>
    /// Applies the <see cref="FundsDepositedEvent"/> to the summary.
    /// </summary>
    /// <param name="event">The funds deposited event.</param>
    /// <param name="view">The account summary view.</param>
    public void Apply(FundsDepositedEvent @event, AccountSummary view)
    {
        view.Balance += @event.Money.Amount;
        view.TransactionCount++;
        view.LastUpdated = @event.OccurredOn;
    }

    /// <summary>
    /// Applies the <see cref="FundsWithdrawnEvent"/> to the summary.
    /// </summary>
    /// <param name="event">The funds withdrawn event.</param>
    /// <param name="view">The account summary view.</param>
    public void Apply(FundsWithdrawnEvent @event, AccountSummary view)
    {
        view.Balance -= @event.Money.Amount;
        view.TransactionCount++;
        view.LastUpdated = @event.OccurredOn;
    }
}
