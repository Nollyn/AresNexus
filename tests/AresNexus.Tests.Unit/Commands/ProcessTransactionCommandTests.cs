using AresNexus.Services.Settlement.Application.Commands;
using AutoFixture;
using FluentAssertions;

namespace AresNexus.Tests.Unit.Commands;

public class ProcessTransactionCommandTests
{
    private readonly IFixture _fixture = TestFixture.Create();

    [Fact]
    public void Should_Create_Command_With_AutoFixture()
    {
        // Act
        var command = _fixture.Create<ProcessTransactionCommand>();

        // Assert
        command.Should().NotBeNull();
        command.AccountId.Should().NotBeEmpty();
        command.Money.Should().NotBeNull();
        command.TransactionType.Should().NotBeNullOrWhiteSpace();
        command.IdempotencyKey.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_Should_Initialize_All_Properties()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var money = _fixture.Create<AresNexus.Services.Settlement.Domain.Money>();
        var type = "DEPOSIT";
        var idempotencyKey = Guid.NewGuid();
        var reference = "Ref123";
        var traceId = "Trace123";
        var correlationId = "Corr123";

        // Act
        var command = new ProcessTransactionCommand(
            accountId, 
            money, 
            type, 
            idempotencyKey, 
            reference, 
            traceId, 
            correlationId);

        // Assert
        command.AccountId.Should().Be(accountId);
        command.Money.Should().Be(money);
        command.TransactionType.Should().Be(type);
        command.IdempotencyKey.Should().Be(idempotencyKey);
        command.Reference.Should().Be(reference);
        command.TraceId.Should().Be(traceId);
        command.CorrelationId.Should().Be(correlationId);
    }
}
