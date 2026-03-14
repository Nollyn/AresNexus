using AresNexus.Services.Settlement.Domain;
using AresNexus.Services.Settlement.Domain.Aggregates;
using AresNexus.Services.Settlement.Domain.Events;
using AresNexus.BuildingBlocks.Domain;
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
        account.Version.Should().Be(0);
        account.GetUncommittedChanges().Should().ContainSingle(e => e is AccountCreatedEvent);
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
        account.Version.Should().Be(1);
        account.GetUncommittedChanges().Should().HaveCount(2);
        account.GetUncommittedChanges().Last().Should().BeOfType<FundsDepositedEvent>();
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
        account.Version.Should().Be(2);
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

    [Fact]
    public void Withdraw_FromLockedAccount_ShouldThrow()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        account.Deposit(new Money(100));
        account.Lock();

        // Act
        var act = () => account.Withdraw(new Money(50));

        // Assert
        act.Should().Throw<AccountLockedException>();
    }

    [Fact]
    public void Deposit_ToLockedAccount_ShouldThrow()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        account.Lock();

        // Act
        var act = () => account.Deposit(new Money(50));

        // Assert
        act.Should().Throw<AccountLockedException>();
    }

    [Fact]
    public void Lock_ShouldBeIdempotent()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        account.Lock();
        var versionBefore = account.Version;

        // Act
        account.Lock();

        // Assert
        account.IsLocked.Should().BeTrue();
        account.Version.Should().Be(versionBefore);
    }

    [Fact]
    public void MultiCurrency_Deposits_ShouldWork()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        
        // Act
        account.Deposit(new Money(100, "CHF"));
        account.Deposit(new Money(50, "CHF"));

        // Assert
        account.Balance.Amount.Should().Be(150);
        account.Balance.Currency.Should().Be("CHF");
    }

    [Fact]
    public void Deposit_WithNegativeAmount_ShouldThrow()
    {
        // Act
        var act = () => new Money(-100);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Amount cannot be negative*");
    }

    [Fact]
    public void Money_WithEmptyCurrency_ShouldThrow()
    {
        // Act
        var act = () => new Money(100, "");

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Currency must be specified*");
    }

    [Fact]
    public void Money_WithDifferentCurrencies_Addition_ShouldThrow()
    {
        // Arrange
        var m1 = new Money(100, "CHF");
        var m2 = new Money(100, "USD");

        // Act
        var act = () => _ = m1 + m2;

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot add money with different currencies");
    }

    [Fact]
    public void Money_WithDifferentCurrencies_Subtraction_ShouldThrow()
    {
        // Arrange
        var m1 = new Money(100, "CHF");
        var m2 = new Money(100, "USD");

        // Act
        var act = () => _ = m1 - m2;

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Cannot subtract money with different currencies");
    }

    [Fact]
    public void Snapshot_ShouldCaptureCurrentState()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        account.Deposit(new Money(100));

        // Act
        var snapshot = account.CreateSnapshot();

        // Assert
        snapshot.Id.Should().Be(account.Id);
        snapshot.Owner.Should().Be(account.Owner);
        snapshot.Balance.Should().Be(account.Balance);
        snapshot.Version.Should().Be(account.Version);
    }

    [Fact]
    public void LoadFromSnapshot_ShouldRestoreState()
    {
        // Arrange
        var account = new Account();
        var snapshot = new Account.Snapshot(Guid.NewGuid(), "Jane Doe", new Money(500), false, 10);

        // Act
        account.LoadFromSnapshot(snapshot);

        // Assert
        account.Id.Should().Be(snapshot.Id);
        account.Owner.Should().Be(snapshot.Owner);
        account.Balance.Should().Be(snapshot.Balance);
        account.Version.Should().Be(snapshot.Version);
    }

    [Fact]
    public void LoadFromSnapshot_ShouldSetCorrectVersion()
    {
        // Arrange
        var account = new Account();
        var snapshot = new Account.Snapshot(Guid.NewGuid(), "Jane Doe", new Money(500), false, 10);

        // Act
        account.LoadFromSnapshot(snapshot);

        // Assert
        account.Version.Should().Be(10);
    }

    [Fact]
    public void LoadFromHistory_ShouldRestoreState()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var history = new List<IDomainEvent>
        {
            new AccountCreatedEvent(accountId, "John Doe", Guid.NewGuid(), DateTime.UtcNow),
            new FundsDepositedEvent(accountId, new Money(100), Guid.NewGuid(), DateTime.UtcNow),
            new FundsWithdrawnEvent(accountId, new Money(30), Guid.NewGuid(), DateTime.UtcNow)
        };
        var account = new Account();

        // Act
        account.LoadsFromHistory(history);

        // Assert
        account.Id.Should().Be(accountId);
        account.Owner.Should().Be("John Doe");
        account.Balance.Amount.Should().Be(70);
        account.Version.Should().Be(2);
        account.GetUncommittedChanges().Should().BeEmpty();
    }

    [Fact]
    public void Withdraw_ExactBalance_ShouldWork()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        account.Deposit(new Money(100));

        // Act
        account.Withdraw(new Money(100));

        // Assert
        account.Balance.Amount.Should().Be(0);
        account.Version.Should().Be(2);
    }

    [Fact]
    public void MultipleOperations_ShouldIncrementVersionCorrectly()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");

        // Act
        account.Deposit(new Money(100));
        account.Withdraw(new Money(50));
        account.Deposit(new Money(200));
        account.Withdraw(new Money(250));

        // Assert
        account.Balance.Amount.Should().Be(0);
        account.Version.Should().Be(4);
    }

    [Fact]
    public void Money_Equality_ShouldWork()
    {
        // Arrange
        var m1 = new Money(100, "CHF");
        var m2 = new Money(100, "CHF");
        var m3 = new Money(200, "CHF");
        var m4 = new Money(100, "USD");

        // Assert
        (m1 == m2).Should().BeTrue();
        (m1 == m3).Should().BeFalse();
        (m1 == m4).Should().BeFalse();
        m1.GetHashCode().Should().Be(m2.GetHashCode());
        m1.GetHashCode().Should().NotBe(m3.GetHashCode());
    }

    [Fact]
    public void LoadFromSnapshot_WithMismatchedVersion_ShouldThrow()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        account.Deposit(new Money(100)); // Version becomes 1
        var snapshot = new Account.Snapshot(account.Id, "John Doe", new Money(200), false, 10);

        // Act
        var act = () => account.LoadFromSnapshot(snapshot);

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*version*");
    }
}
