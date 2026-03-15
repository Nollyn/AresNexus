using AresNexus.Services.Settlement.Domain;
using AresNexus.Services.Settlement.Domain.Events;
using AresNexus.Services.Settlement.Infrastructure.Projections;
using FluentAssertions;

namespace AresNexus.Tests.Unit;

public class AccountSummaryProjectionTests
{
    private readonly AccountSummaryProjection _sut = new();

    [Fact]
    public void Apply_AccountCreatedEvent_ShouldInitializeView()
    {
        // Arrange
        var view = new AccountSummary();
        var accountId = Guid.NewGuid();
        var occurredOn = DateTime.UtcNow;
        var @event = new AccountCreatedEvent(accountId, "Owner", Guid.NewGuid(), occurredOn);

        // Act
        _sut.Apply(@event, view);

        // Assert
        view.Id.Should().Be(accountId);
        view.Owner.Should().Be("Owner");
        view.Balance.Should().Be(0);
        view.TransactionCount.Should().Be(0);
        view.LastUpdated.Should().Be(occurredOn);
    }

    [Fact]
    public void Apply_FundsDepositedEvent_ShouldUpdateBalanceAndCount()
    {
        // Arrange
        var view = new AccountSummary { Balance = 100, TransactionCount = 1 };
        var money = new Money(50, "CHF");
        var occurredOn = DateTime.UtcNow;
        var @event = new FundsDepositedEvent(Guid.NewGuid(), money, Guid.NewGuid(), occurredOn);

        // Act
        _sut.Apply(@event, view);

        // Assert
        view.Balance.Should().Be(150);
        view.TransactionCount.Should().Be(2);
        view.LastUpdated.Should().Be(occurredOn);
    }

    [Fact]
    public void Apply_FundsWithdrawnEvent_ShouldUpdateBalanceAndCount()
    {
        // Arrange
        var view = new AccountSummary { Balance = 100, TransactionCount = 1 };
        var money = new Money(50, "CHF");
        var occurredOn = DateTime.UtcNow;
        var @event = new FundsWithdrawnEvent(Guid.NewGuid(), money, Guid.NewGuid(), occurredOn);

        // Act
        _sut.Apply(@event, view);

        // Assert
        view.Balance.Should().Be(50);
        view.TransactionCount.Should().Be(2);
        view.LastUpdated.Should().Be(occurredOn);
    }
}
