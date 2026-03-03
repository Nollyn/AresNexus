using AresNexus.Settlement.Domain;
using AresNexus.Shared.Kernel;
using FluentAssertions;
using Xunit;

namespace AresNexus.Settlement.Tests;

public class MoneyTests
{
    [Fact]
    public void Constructor_ShouldRoundToTwoDecimalPlaces()
    {
        // Arrange
        var amount = 100.1234m;

        // Act
        var money = new Money(amount, CurrencyConstants.Chf);

        // Assert
        money.Amount.Should().Be(100.12m);
    }

    [Fact]
    public void Constructor_AwayFromZeroRounding_ShouldWork()
    {
        // Arrange
        var amount = 100.125m;

        // Act
        var money = new Money(amount, CurrencyConstants.Chf);

        // Assert: 100.125 -> 100.13
        money.Amount.Should().Be(100.13m);
    }

    [Fact]
    public void Addition_MismatchedCurrency_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var chf = new Money(100, CurrencyConstants.Chf);
        var eur = new Money(100, "EUR");

        // Act
        Action act = () => { var _ = chf + eur; };

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot add money with different currencies");
    }

    [Fact]
    public void Subtraction_MismatchedCurrency_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var chf = new Money(100, CurrencyConstants.Chf);
        var eur = new Money(100, "EUR");

        // Act
        Action act = () => { var _ = chf - eur; };

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot subtract money with different currencies");
    }

    [Fact]
    public void Constructor_NegativeAmount_ShouldThrowArgumentException()
    {
        // Act
        Action act = () => new Money(-10);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Amount cannot be negative*");
    }
}
