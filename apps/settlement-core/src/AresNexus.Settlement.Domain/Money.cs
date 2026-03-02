using AresNexus.Shared.Kernel;

namespace AresNexus.Settlement.Domain;

/// <summary>
/// Represents a monetary value in a specific currency.
/// </summary>
public record Money
{
    /// <summary>Gets the amount of money.</summary>
    public decimal Amount { get; init; }
    /// <summary>Gets the ISO currency code.</summary>
    public string Currency { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Money"/> record.
    /// </summary>
    /// <param name="amount">The amount.</param>
    /// <param name="currency">The currency code.</param>
    public Money(decimal amount, string currency = CurrencyConstants.Chf)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency must be specified", nameof(currency));
        }

        Amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    /// <summary>
    /// Adds two monetary values together.
    /// </summary>
    /// <param name="a">The first monetary value.</param>
    /// <param name="b">The second monetary value.</param>
    /// <returns>The sum of the two monetary values.</returns>
    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException("Cannot add money with different currencies");
        }

        return new Money(a.Amount + b.Amount, a.Currency);
    }

    /// <summary>
    /// Subtracts one monetary value from another.
    /// </summary>
    /// <param name="a">The first monetary value.</param>
    /// <param name="b">The second monetary value.</param>
    /// <returns>The difference between the two monetary values.</returns>
    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
        {
            throw new InvalidOperationException("Cannot subtract money with different currencies");
        }

        return new Money(a.Amount - b.Amount, a.Currency);
    }
}
