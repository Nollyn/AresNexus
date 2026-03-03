using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain;
using AresNexus.Settlement.Infrastructure.Idempotency;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AresNexus.Tests.Unit;

public class IdempotencyTests
{
    private readonly Mock<IIdempotencyStore> _idempotencyStoreMock;
    private readonly Mock<ILogger<CommandIdempotencyBehavior<ProcessTransactionCommand, bool>>> _loggerMock;
    private readonly CommandIdempotencyBehavior<ProcessTransactionCommand, bool> _behavior;

    public IdempotencyTests()
    {
        _idempotencyStoreMock = new Mock<IIdempotencyStore>();
        _loggerMock = new Mock<ILogger<CommandIdempotencyBehavior<ProcessTransactionCommand, bool>>>();
        _behavior = new CommandIdempotencyBehavior<ProcessTransactionCommand, bool>(
            _idempotencyStoreMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenKeyExists_ShouldReturnCachedResultAndNotCallNext()
    {
        // Arrange
        var command = new ProcessTransactionCommand(
            Guid.NewGuid(),
            new Money(100),
            "DEPOSIT",
            Guid.NewGuid());

        _idempotencyStoreMock.Setup(s => s.ExistsAsync(command.IdempotencyKey))
            .ReturnsAsync(true);
        _idempotencyStoreMock.Setup(s => s.GetAsync<bool>(command.IdempotencyKey))
            .ReturnsAsync(true);

        var nextCalled = false;
        RequestHandlerDelegate<bool> next = (CancellationToken ct) =>
        {
            nextCalled = true;
            return Task.FromResult(false);
        };

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        nextCalled.Should().BeFalse();
        _idempotencyStoreMock.Verify(s => s.GetAsync<bool>(command.IdempotencyKey), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenKeyDoesNotExist_ShouldCallNextAndStoreResult()
    {
        // Arrange
        var command = new ProcessTransactionCommand(
            Guid.NewGuid(),
            new Money(100),
            "DEPOSIT",
            Guid.NewGuid());

        _idempotencyStoreMock.Setup(s => s.ExistsAsync(command.IdempotencyKey))
            .ReturnsAsync(false);

        var nextCalled = false;
        RequestHandlerDelegate<bool> next = (CancellationToken ct) =>
        {
            nextCalled = true;
            return Task.FromResult(true);
        };

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        nextCalled.Should().BeTrue();
        _idempotencyStoreMock.Verify(s => s.StoreAsync(command.IdempotencyKey, true), Times.Once);
    }
}
