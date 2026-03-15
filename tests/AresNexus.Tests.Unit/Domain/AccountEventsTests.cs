using AresNexus.Services.Settlement.Domain;
using AresNexus.Services.Settlement.Domain.Events;
using AutoFixture;
using FluentAssertions;

namespace AresNexus.Tests.Unit.Domain;

public class AccountEventsTests
{
    private readonly IFixture _fixture = TestFixture.Create();

    [Fact]
    public void AccountCreatedEvent_Should_Be_Creatable_By_AutoFixture()
    {
        // Act
        var @event = _fixture.Create<AccountCreatedEvent>();

        // Assert
        @event.Should().NotBeNull();
        @event.AccountId.Should().NotBeEmpty();
        @event.Owner.Should().NotBeNullOrWhiteSpace();
        @event.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public void FundsDepositedEvent_Should_Be_Creatable_By_AutoFixture()
    {
        // Act
        var @event = _fixture.Create<FundsDepositedEvent>();

        // Assert
        @event.Should().NotBeNull();
        @event.AccountId.Should().NotBeEmpty();
        @event.Money.Should().NotBeNull();
        @event.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public void FundsWithdrawnEvent_Should_Be_Creatable_By_AutoFixture()
    {
        // Act
        var @event = _fixture.Create<FundsWithdrawnEvent>();

        // Assert
        @event.Should().NotBeNull();
        @event.AccountId.Should().NotBeEmpty();
        @event.Money.Should().NotBeNull();
        @event.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public void AccountLockedEvent_Should_Be_Creatable_By_AutoFixture()
    {
        // Act
        var @event = _fixture.Create<AccountLockedEvent>();

        // Assert
        @event.Should().NotBeNull();
        @event.AccountId.Should().NotBeEmpty();
        @event.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public void AccountCreatedEvent_Constructor_Should_Initialize_Properties()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var owner = "Owner1";
        var eventId = Guid.NewGuid();
        var occurredOn = DateTime.UtcNow;
        var traceId = "T1";
        var correlationId = "C1";

        // Act
        var @event = new AccountCreatedEvent(accountId, owner, eventId, occurredOn, 1, traceId, correlationId);

        // Assert
        @event.AccountId.Should().Be(accountId);
        @event.Owner.Should().Be(owner);
        @event.EventId.Should().Be(eventId);
        @event.OccurredOn.Should().Be(occurredOn);
        @event.SchemaVersion.Should().Be(1);
        @event.TraceId.Should().Be(traceId);
        @event.CorrelationId.Should().Be(correlationId);
    }
}
