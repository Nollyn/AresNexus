using AresNexus.Settlement.Domain;
using AresNexus.Settlement.Domain.Aggregates;
using FluentAssertions;
using Xunit;

namespace AresNexus.Tests.Unit;

public class AccountTests
{
    [Fact]
    public void NewAccount_ShouldHaveZeroBalance()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var owner = "John Doe";

        // Act
        var account = new Account(accountId, owner);

        // Assert
        account.Id.Should().Be(accountId);
        account.Owner.Should().Be(owner);
        account.Balance.Amount.Should().Be(0);
    }

    [Fact]
    public void Deposit_ShouldIncreaseBalance()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        var depositAmount = new Money(100);

        // Act
        account.Deposit(depositAmount);

        // Assert
        account.Balance.Amount.Should().Be(100);
    }

    [Fact]
    public void Withdraw_ShouldDecreaseBalance()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        account.Deposit(new Money(100));

        // Act
        account.Withdraw(new Money(40));

        // Assert
        account.Balance.Amount.Should().Be(60);
    }

    [Fact]
    public void Withdraw_WithInsufficientFunds_ShouldThrow()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        account.Deposit(new Money(50));

        // Act
        var act = () => account.Withdraw(new Money(100));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Insufficient funds");
    }
}
