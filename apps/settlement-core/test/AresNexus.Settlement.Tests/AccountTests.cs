using AresNexus.Settlement.Domain;
using AresNexus.Settlement.Domain.Aggregates;
using AresNexus.Settlement.Domain.Events;
using FluentAssertions;
using Xunit;

namespace AresNexus.Settlement.Tests;

public class AccountTests
{
    [Fact]
    public void Withdraw_ExactBalance_ShouldSucceed()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var account = new Account(accountId, "John Doe");
        var amount = new Money(100);
        account.Deposit(amount);

        // Act
        account.Withdraw(amount);

        // Assert
        account.Balance.Amount.Should().Be(0);
        var events = account.GetUncommittedChanges();
        events.Should().ContainItemsAssignableTo<FundsWithdrawnEvent>();
    }

    [Fact]
    public void Deposit_LockedAccount_ShouldThrowAccountLockedException()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        account.Lock();

        // Act
        Action act = () => account.Deposit(new Money(100));

        // Assert
        act.Should().Throw<AccountLockedException>()
            .WithMessage($"Account {account.Id} is locked.");
    }

    [Fact]
    public void Withdraw_LockedAccount_ShouldThrowAccountLockedException()
    {
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        account.Deposit(new Money(100));
        account.Lock();

        // Act
        Action act = () => account.Withdraw(new Money(50));

        // Assert
        act.Should().Throw<AccountLockedException>()
            .WithMessage($"Account {account.Id} is locked.");
    }

    [Fact]
    public void VersionMismatch_ShouldSimulateMartenOptimisticConcurrency()
    {
        // Marten's optimistic concurrency usually throws a ConcurrencyException 
        // when the version in the database doesn't match the expected version.
        // Here we simulate the logic where we expect a specific version.
        
        // Arrange
        var account = new Account(Guid.NewGuid(), "John Doe");
        account.Deposit(new Money(100));
        var initialVersion = account.Version;

        // Simulate another process updating the account
        var concurrentAccount = new Account();
        concurrentAccount.LoadFromSnapshot(account.CreateSnapshot());
        concurrentAccount.Deposit(new Money(50));
        // In a real scenario, concurrentAccount would be saved, incrementing version in DB to 1
        
        // Act & Assert
        // If we try to save 'account' with version 0, but DB has version 1, it should fail.
        // This is handled by Marten, but we can verify our Version tracking.
        account.Version.Should().Be(initialVersion);
        concurrentAccount.Version.Should().Be(initialVersion + 1);
    }
}
