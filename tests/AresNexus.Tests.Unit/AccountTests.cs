using AresNexus.Settlement.Domain;
using AresNexus.Settlement.Domain.Aggregates;
using AresNexus.Settlement.Domain.Events;
using AresNexus.Shared.Kernel;
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
        var snapshot = new Account.Snapshot(Guid.NewGuid(), "Jane Doe", new Money(500), 10);

        // Act
        account.LoadFromSnapshot(snapshot);

        // Assert
        account.Id.Should().Be(snapshot.Id);
        account.Owner.Should().Be(snapshot.Owner);
        account.Balance.Should().Be(snapshot.Balance);
        account.Version.Should().Be(snapshot.Version);
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
}
