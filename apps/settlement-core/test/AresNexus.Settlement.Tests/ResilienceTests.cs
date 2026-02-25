using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Application.Handlers;
using AresNexus.Settlement.Domain.Aggregates;
using AresNexus.Settlement.Domain.Events;
using AresNexus.Settlement.Infrastructure.EventStore;
using AresNexus.Settlement.Infrastructure.Idempotency;
using AresNexus.Settlement.Infrastructure.Security;
using AresNexus.Settlement.Infrastructure.Repositories;
using Xunit;

namespace AresNexus.Settlement.Tests;

public class ResilienceTests
{
    private readonly InMemoryCosmosEventStore _eventStore = new();
    private readonly InMemoryAccountRepository _repository;
    private readonly InMemoryIdempotencyStore _idempotencyStore = new();
    private readonly MockEncryptionService _encryptionService = new();
    private readonly ProcessTransactionCommandHandler _handler;

    public ResilienceTests()
    {
        _repository = new InMemoryAccountRepository(_eventStore);
        _handler = new ProcessTransactionCommandHandler(_repository, _idempotencyStore, _encryptionService);
    }

    [Fact]
    public async Task ProcessTransaction_ShouldBeIdempotent()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        var cmd = new ProcessTransactionCommand(accountId, 100, "DEPOSIT", idempotencyKey);

        // Act
        var result1 = await _handler.Handle(cmd, CancellationToken.None);
        var result2 = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        
        var events = await _eventStore.GetEventsAsync(accountId);
        // We expect 2 events: AccountCreatedEvent (from the handler's logic) and FundsDepositedEvent
        Assert.Equal(2, events.Count); 
    }

    [Fact]
    public async Task ProcessTransaction_ShouldEncryptReference()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var cmd = new ProcessTransactionCommand(accountId, 100, "DEPOSIT", Guid.NewGuid(), "Secret Reference");

        // Act
        await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        var events = await _eventStore.GetEventsAsync(accountId);
        var depositEvent = events.OfType<FundsDepositedEvent>().First();
        Assert.StartsWith("ENC:", depositEvent.Reference);
    }

    [Fact]
    public async Task Snapshotting_ShouldSaveSnapshot_After50Events()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        
        // Act: Process 51 transactions
        for (var i = 0; i < 51; i++)
        {
            var cmd = new ProcessTransactionCommand(accountId, 1, "DEPOSIT", Guid.NewGuid());
            await _handler.Handle(cmd, CancellationToken.None);
        }

        // Assert
        var (snapshot, version) = await _eventStore.GetLatestSnapshotAsync<Account.Snapshot>(accountId);
        Assert.NotNull(snapshot);
        Assert.Equal(50, version); // Snapshot taken at version 50 (after 51st event is applied? No, 0-indexed version 50 is 51st event)
    }

    [Fact]
    public async Task Outbox_ShouldStoreMessage()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var cmd = new ProcessTransactionCommand(accountId, 100, "DEPOSIT", Guid.NewGuid());

        // Act
        await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        var messages = _eventStore.GetUnprocessedOutboxMessages();
        Assert.Single(messages);
    }
}
