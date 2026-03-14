using AresNexus.Services.Settlement.Domain;
using FluentAssertions;
using Xunit;

namespace AresNexus.Tests.Unit;

public class MoneyTests
{
    [Fact]
    public void NewMoney_ShouldSetProperties()
    {
        // Act
        var money = new Money(100.55m, "USD");

        // Assert
        money.Amount.Should().Be(100.55m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void NewMoney_ShouldRoundToTwoDecimals()
    {
        // Act
        var money = new Money(100.555m);

        // Assert
        money.Amount.Should().Be(100.56m);
    }

    [Fact]
    public void NewMoney_WithNegativeAmount_ShouldThrow()
    {
        // Act
        var act = () => new Money(-1);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Amount cannot be negative*");
    }

    [Fact]
    public void NewMoney_WithNullCurrency_ShouldThrow()
    {
        // Act
        var act = () => new Money(100, null!);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Currency must be specified*");
    }

    [Fact]
    public void Addition_ShouldSumAmounts()
    {
        // Arrange
        var m1 = new Money(100);
        var m2 = new Money(50);

        // Act
        var result = m1 + m2;

        // Assert
        result.Amount.Should().Be(150);
        result.Currency.Should().Be(m1.Currency);
    }

    [Fact]
    public void Addition_WithDifferentCurrencies_ShouldThrow()
    {
        // Arrange
        var m1 = new Money(100, "CHF");
        var m2 = new Money(50, "USD");

        // Act
        var act = () => m1 + m2;

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot add money with different currencies");
    }

    [Fact]
    public void Subtraction_ShouldSubtractAmounts()
    {
        // Arrange
        var m1 = new Money(100);
        var m2 = new Money(40);

        // Act
        var result = m1 - m2;

        // Assert
        result.Amount.Should().Be(60);
    }

    [Fact]
    public void Subtraction_WithDifferentCurrencies_ShouldThrow()
    {
        // Arrange
        var m1 = new Money(100, "CHF");
        var m2 = new Money(40, "USD");

        // Act
        var act = () => m1 - m2;

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot subtract money with different currencies");
    }

    [Fact]
    public void Equality_ShouldWork()
    {
        // Arrange
        var m1 = new Money(100, "CHF");
        var m2 = new Money(100, "CHF");
        var m3 = new Money(101, "CHF");
        var m4 = new Money(100, "USD");

        // Assert
        (m1 == m2).Should().BeTrue();
        (m1 != m3).Should().BeTrue();
        (m1 == m4).Should().BeFalse();
        m1.Equals(m2).Should().BeTrue();
        m1.GetHashCode().Should().Be(m2.GetHashCode());
    }
}
