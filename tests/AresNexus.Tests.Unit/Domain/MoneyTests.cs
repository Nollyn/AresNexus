using AresNexus.Services.Settlement.Domain;
using AutoFixture;
using FluentAssertions;

namespace AresNexus.Tests.Unit.Domain;

public class MoneyTests
{
    private readonly IFixture _fixture = TestFixture.Create();

    [Fact]
    public void Should_Create_Money_With_AutoFixture()
    {
        // Act
        var money = _fixture.Create<Money>();

        // Assert
        money.Should().NotBeNull();
        money.Amount.Should().BeGreaterThanOrEqualTo(0);
        money.Currency.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Constructor_Should_Initialize_Properties()
    {
        // Arrange
        var amount = 100.50m;
        var currency = "USD";

        // Act
        var money = new Money(amount, currency);

        // Assert
        money.Amount.Should().Be(amount);
        money.Currency.Should().Be(currency);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Amount_Is_Negative()
    {
        // Act
        Action act = () => new Money(-1m, "CHF");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Amount cannot be negative*");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Currency_Is_Null()
    {
        // Act
        Action act = () => new Money(1m, null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Currency must be specified*");
    }

    [Fact]
    public void Addition_Operator_Should_Work_For_Same_Currency()
    {
        // Arrange
        var m1 = new Money(10m, "CHF");
        var m2 = new Money(20m, "CHF");

        // Act
        var result = m1 + m2;

        // Assert
        result.Amount.Should().Be(30m);
        result.Currency.Should().Be("CHF");
    }

    [Fact]
    public void Addition_Operator_Should_Throw_For_Different_Currencies()
    {
        // Arrange
        var m1 = new Money(10m, "CHF");
        var m2 = new Money(20m, "USD");

        // Act
        Action act = () => { var _ = m1 + m2; };

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot add money with different currencies*");
    }

    [Fact]
    public void Subtraction_Operator_Should_Work_For_Same_Currency()
    {
        // Arrange
        var m1 = new Money(30m, "CHF");
        var m2 = new Money(10m, "CHF");

        // Act
        var result = m1 - m2;

        // Assert
        result.Amount.Should().Be(20m);
        result.Currency.Should().Be("CHF");
    }

    [Fact]
    public void Subtraction_Operator_Should_Throw_For_Different_Currencies()
    {
        // Arrange
        var m1 = new Money(30m, "CHF");
        var m2 = new Money(10m, "USD");

        // Act
        Action act = () => { var _ = m1 - m2; };

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot subtract money with different currencies*");
    }

    [Fact]
    public void Equality_Should_Be_Based_On_Value()
    {
        // Arrange
        var m1 = new Money(100m, "CHF");
        var m2 = new Money(100m, "CHF");
        var m3 = new Money(200m, "CHF");

        // Assert
        m1.Should().Be(m2);
        m1.Should().NotBe(m3);
    }
}
