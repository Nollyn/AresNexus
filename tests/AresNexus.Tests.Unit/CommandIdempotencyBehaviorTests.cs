using AresNexus.Services.Settlement.Application.Commands;
using AresNexus.Services.Settlement.Application.Interfaces;
using AresNexus.Services.Settlement.Domain;
using AresNexus.Services.Settlement.Infrastructure.Idempotency;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace AresNexus.Tests.Unit;

public class CommandIdempotencyBehaviorTests
{
    private readonly Mock<IIdempotencyStore> _storeMock;
    private readonly Mock<ILogger<CommandIdempotencyBehavior<ProcessTransactionCommand, bool>>> _loggerMock;
    private readonly CommandIdempotencyBehavior<ProcessTransactionCommand, bool> _behavior;

    public CommandIdempotencyBehaviorTests()
    {
        _storeMock = new Mock<IIdempotencyStore>();
        _loggerMock = new Mock<ILogger<CommandIdempotencyBehavior<ProcessTransactionCommand, bool>>>();
        _behavior = new CommandIdempotencyBehavior<ProcessTransactionCommand, bool>(_storeMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenKeyAlreadyExists_ShouldNotCallHandler()
    {
        // Arrange
        var command = new ProcessTransactionCommand(Guid.NewGuid(), new Money(100), "DEPOSIT", Guid.NewGuid());
        bool handlerCalled = false;
        RequestHandlerDelegate<bool> next = (CancellationToken _) => 
        {
            handlerCalled = true;
            return Task.FromResult(true);
        };

        _storeMock.Setup(x => x.ExistsAsync(command.IdempotencyKey)).ReturnsAsync(true);
        _storeMock.Setup(x => x.GetAsync<bool>(command.IdempotencyKey)).ReturnsAsync(true);

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        handlerCalled.Should().BeFalse();
        _storeMock.Verify(x => x.GetAsync<bool>(command.IdempotencyKey), Times.Once);
        _storeMock.Verify(x => x.StoreAsync(It.IsAny<Guid>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenKeyIsNew_ShouldCallHandlerAndCacheResult()
    {
        // Arrange
        var command = new ProcessTransactionCommand(Guid.NewGuid(), new Money(100), "DEPOSIT", Guid.NewGuid());
        bool handlerCalled = false;
        RequestHandlerDelegate<bool> next = (CancellationToken _) => 
        {
            handlerCalled = true;
            return Task.FromResult(true);
        };

        _storeMock.Setup(x => x.ExistsAsync(command.IdempotencyKey)).ReturnsAsync(false);

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        handlerCalled.Should().BeTrue();
        _storeMock.Verify(x => x.StoreAsync(command.IdempotencyKey, true), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequestIsNotProcessTransactionCommand_ShouldCallHandlerDirectly()
    {
        // Arrange
        var request = new Mock<IRequest<bool>>().Object;
        bool handlerCalled = false;
        RequestHandlerDelegate<bool> next = (CancellationToken _) => 
        {
            handlerCalled = true;
            return Task.FromResult(true);
        };

        // Create behavior for a generic IRequest<bool>
        var behavior = new CommandIdempotencyBehavior<IRequest<bool>, bool>(_storeMock.Object, new Mock<ILogger<CommandIdempotencyBehavior<IRequest<bool>, bool>>>().Object);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        handlerCalled.Should().BeTrue();
        _storeMock.Verify(x => x.ExistsAsync(It.IsAny<Guid>()), Times.Never);
    }
}
