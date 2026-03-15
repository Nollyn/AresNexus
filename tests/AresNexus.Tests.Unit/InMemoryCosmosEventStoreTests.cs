using AresNexus.BuildingBlocks.Domain;
using AresNexus.Services.Settlement.Infrastructure.EventStore;
using FluentAssertions;
using Moq;

namespace AresNexus.Tests.Unit;

public class InMemoryCosmosEventStoreTests
{
    private readonly InMemoryCosmosEventStore _sut = new();

    [Fact]
    public async Task GetEventsAsync_ShouldReturnAllEvents_WhenFromVersionIsMinusOne()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var ev = new TestEvent(Guid.NewGuid(), DateTime.UtcNow);
        await _sut.SaveEventsAsync(aggregateId, [ev], -1);

        // Act
        var events = await _sut.GetEventsAsync(aggregateId, -1);

        // Assert
        events.Should().HaveCount(1);
        events.First().EventId.Should().Be(ev.EventId);
    }

    [Fact]
    public async Task GetEventsAsync_ShouldReturnEmpty_WhenAggregateDoesNotExist()
    {
        // Act
        var events = await _sut.GetEventsAsync(Guid.NewGuid());

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldThrow_WhenConcurrencyConflictDetected()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var ev1 = new TestEvent(Guid.NewGuid(), DateTime.UtcNow);
        await _sut.SaveEventsAsync(aggregateId, [ev1], -1);

        // Act & Assert
        var ev2 = new TestEvent(Guid.NewGuid(), DateTime.UtcNow);
        // Current version is 0 (one event added), so expectedVersion should be 0 to succeed.
        // Providing -1 or anything else other than 0 should throw if we want to test conflict.
        // Wait, the code says: if (currentVersion != expectedVersion && expectedVersion != -1)
        // If expectedVersion is -1, it bypasses the check.
        // To trigger the exception, expectedVersion must be != -1 AND != currentVersion.
        await _sut.Awaiting(s => s.SaveEventsAsync(aggregateId, [ev2], 1))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Concurrency conflict detected while saving events.");
    }

    [Fact]
    public async Task SaveSnapshotAsync_ShouldStoreSnapshot()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var snapshot = new { State = "Snapshot" };
        var version = 5;

        // Act
        await _sut.SaveSnapshotAsync(aggregateId, snapshot, version);

        // Assert
        var (storedSnapshot, storedVersion) = await _sut.GetLatestSnapshotAsync<object>(aggregateId);
        storedSnapshot.Should().Be(snapshot);
        storedVersion.Should().Be(version);
    }

    [Fact]
    public async Task GetLatestSnapshotAsync_ShouldReturnDefault_WhenNoSnapshotExists()
    {
        // Act
        var (snapshot, version) = await _sut.GetLatestSnapshotAsync<object>(Guid.NewGuid());

        // Assert
        snapshot.Should().BeNull();
        version.Should().Be(-1);
    }

    [Fact]
    public async Task GetUnprocessedOutboxMessages_ShouldReturnUnprocessedMessages()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var ev = new TestEvent(Guid.NewGuid(), DateTime.UtcNow);
        await _sut.SaveChangesAsync(aggregateId, [ev], -1, [new { Msg = "test" }]);

        // Act
        var messages = _sut.GetUnprocessedOutboxMessages();

        // Assert
        messages.Should().HaveCount(2); // One for the event, one for the outbox message
        messages.Should().AllSatisfy(m => m.ProcessedOnUtc.Should().BeNull());
    }

    [Fact]
    public async Task SaveSnapshotAsync_ShouldThrow_WhenSnapshotIsNull()
    {
        // Act & Assert
        await _sut.Awaiting(s => s.SaveSnapshotAsync<object>(Guid.NewGuid(), null!, 0))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    private record TestEvent(Guid EventId, DateTime OccurredOn, int SchemaVersion = 1, string? TraceId = null, string? CorrelationId = null) : IDomainEvent;
}
