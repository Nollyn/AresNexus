using System.Text;
using System.Text.Json;
using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain;
using AresNexus.Settlement.Infrastructure.Idempotency;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
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
    public async Task Handle_WhenRequestIsNotProcessTransactionCommand_ShouldCallNextDirectly()
    {
        // Arrange
        var request = new Mock<IRequest<bool>>().Object;
        var nextCalled = false;
        RequestHandlerDelegate<bool> next = (CancellationToken ct) =>
        {
            nextCalled = true;
            return Task.FromResult(true);
        };

        var behavior = new CommandIdempotencyBehavior<IRequest<bool>, bool>(
            _idempotencyStoreMock.Object,
            new Mock<ILogger<CommandIdempotencyBehavior<IRequest<bool>, bool>>>().Object);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        nextCalled.Should().BeTrue();
        _idempotencyStoreMock.Verify(s => s.ExistsAsync(It.IsAny<Guid>()), Times.Never);
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

    [Fact]
    public async Task Handle_WhenKeyExists_ShouldReturnCachedResult_AndNotCallNext()
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
            return Task.FromResult(false); // Should not be called
        };

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        nextCalled.Should().BeFalse();
        _idempotencyStoreMock.Verify(s => s.GetAsync<bool>(command.IdempotencyKey), Times.Once);
    }

    [Fact]
    public async Task RedisIdempotencyStore_ExistsAsync_WhenKeyExists_ShouldReturnTrue()
    {
        // Arrange
        var cacheMock = new Mock<IDistributedCache>();
        var key = Guid.NewGuid();
        cacheMock.Setup(c => c.GetAsync(key.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([1]);
        var store = new RedisIdempotencyStore(cacheMock.Object);

        // Act
        var result = await store.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RedisIdempotencyStore_StoreAsync_ShouldSetCache()
    {
        // Arrange
        var cacheMock = new Mock<IDistributedCache>();
        var key = Guid.NewGuid();
        var result = true;
        var store = new RedisIdempotencyStore(cacheMock.Object);

        // Act
        await store.StoreAsync(key, result);

        // Assert
        cacheMock.Verify(c => c.SetAsync(
            key.ToString(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RedisIdempotencyStore_GetAsync_ShouldReturnDeserializedResult()
    {
        // Arrange
        var cacheMock = new Mock<IDistributedCache>();
        var key = Guid.NewGuid();
        var resultValue = true;
        var json = JsonSerializer.Serialize(resultValue);
        cacheMock.Setup(c => c.GetAsync(key.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Encoding.UTF8.GetBytes(json));
        var store = new RedisIdempotencyStore(cacheMock.Object);

        // Act
        var result = await store.GetAsync<bool>(key);

        // Assert
        result.Should().BeTrue();
    }
}
