using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Application.Handlers;
using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Domain;
using AresNexus.Settlement.Domain.Aggregates;
using AresNexus.Settlement.Domain.Events;
using AresNexus.Settlement.Infrastructure.EventStore;
using AresNexus.Settlement.Infrastructure.Idempotency;
using AresNexus.Settlement.Infrastructure.Security;
using AresNexus.Settlement.Infrastructure.Repositories;
using Xunit;

namespace AresNexus.Settlement.Tests;

/// <summary>
/// Resilience and Persistence tests for Swiss Tier-1 Banking.
/// </summary>
public class ResilienceTests
{
    private readonly InMemoryCosmosEventStore _eventStore = new();
    private readonly InMemoryAccountRepository _repository;
    private readonly InMemoryIdempotencyStore _idempotencyStore = new();
    private readonly MockEncryptionService _encryptionService = new();
    private readonly IKeyVaultClient _keyVaultClient = new MockKeyVaultClient();
    private readonly System.Diagnostics.Metrics.Meter _meter = new("TestMeter", "1.0.0");
    private readonly ProcessTransactionCommandHandler _handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceTests"/> class.
    /// </summary>
    public ResilienceTests()
    {
        _repository = new InMemoryAccountRepository(_eventStore);
        // Handler uses IAccountRepository, IEncryptionService, IKeyVaultClient and Meter.
        _handler = new ProcessTransactionCommandHandler(_repository, _encryptionService, _keyVaultClient, _meter);
    }

    /// <summary>
    /// Verifies that processing the same transaction twice with the same IdempotencyKey is handled.
    /// Note: This test focuses on the handler's ability to save correctly. 
    /// Full idempotency is verified via the Pipeline Behavior in integration tests.
    /// </summary>
    [Fact]
    public async Task ProcessTransaction_ShouldSaveCorrectly()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid();
        var cmd = new ProcessTransactionCommand(accountId, new Money(100), "DEPOSIT", idempotencyKey);

        // Act
        var result1 = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        Assert.True(result1);
        
        var events = await _eventStore.GetEventsAsync(accountId);
        // We expect 2 events: AccountCreatedEvent (from the handler's logic) and FundsDepositedEvent
        Assert.Equal(2, events.Count); 
    }

    /// <summary>
    /// Verifies that the PII encryption is applied to the reference field.
    /// </summary>
    [Fact]
    public async Task ProcessTransaction_ShouldEncryptReference()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var cmd = new ProcessTransactionCommand(accountId, new Money(100), "DEPOSIT", Guid.NewGuid(), "Secret Reference");

        // Act
        await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        var events = await _eventStore.GetEventsAsync(accountId);
        var depositEvent = events.OfType<FundsDepositedEvent>().First();
        // Since we now use KeyVault after EncryptionService, the prefix will be different.
        Assert.Contains("KVAULT:", depositEvent.Reference);
    }

    /// <summary>
    /// Verifies that snapshotting is triggered after 100 events.
    /// </summary>
    [Fact]
    public async Task Snapshotting_ShouldSaveSnapshot_After100Events()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        
        // Act: Process 101 transactions (1 AccountCreated + 100 Deposits)
        // First one creates the account and 1st deposit (2 events)
        await _handler.Handle(new ProcessTransactionCommand(accountId, new Money(1), "DEPOSIT", Guid.NewGuid()), CancellationToken.None);
        
        // Process 99 more deposits
        for (var i = 0; i < 99; i++)
        {
            var cmd = new ProcessTransactionCommand(accountId, new Money(1), "DEPOSIT", Guid.NewGuid());
            await _handler.Handle(cmd, CancellationToken.None);
        }

        // Total events: 101. The 100th event (version 99) or above should trigger a snapshot.
        // Assert
        var (snapshot, version) = await _eventStore.GetLatestSnapshotAsync<Account.Snapshot>(accountId);
        Assert.NotNull(snapshot);
        Assert.True(version >= 99); 
    }

    /// <summary>
    /// Verifies that withdrawing with insufficient funds throws an exception.
    /// </summary>
    [Fact]
    public async Task ProcessTransaction_WithdrawWithInsufficientFunds_ShouldThrow()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        // First deposit 50
        await _handler.Handle(new ProcessTransactionCommand(accountId, new Money(50), "DEPOSIT", Guid.NewGuid()), CancellationToken.None);
        
        // Act: Withdraw 100
        var cmd = new ProcessTransactionCommand(accountId, new Money(100), "WITHDRAW", Guid.NewGuid());
        var act = () => _handler.Handle(cmd, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    /// <summary>
    /// Verifies that an unknown transaction type throws an exception.
    /// </summary>
    [Fact]
    public async Task ProcessTransaction_UnknownType_ShouldThrow()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var cmd = new ProcessTransactionCommand(accountId, new Money(100), "INVALID", Guid.NewGuid());

        // Act
        var act = () => _handler.Handle(cmd, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(act);
    }

    /// <summary>
    /// Verifies that outbox messages are stored when a transaction is processed.
    /// </summary>
    [Fact]
    public async Task Outbox_ShouldStoreMessage()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var cmd = new ProcessTransactionCommand(accountId, new Money(100), "DEPOSIT", Guid.NewGuid());

        // Act
        await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        var messages = _eventStore.GetUnprocessedOutboxMessages();
        Assert.NotEmpty(messages);
    }
}
