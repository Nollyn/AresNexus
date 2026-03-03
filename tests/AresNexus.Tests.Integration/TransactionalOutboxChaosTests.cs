using AresNexus.Settlement.Domain;
using AresNexus.Settlement.Domain.Aggregates;
using AresNexus.Settlement.Infrastructure.Repositories;
using Marten;
using Marten.Events;
using Moq;
using Xunit;
using FluentAssertions;
using System.Text.Json;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Infrastructure.Messaging;
using AresNexus.Shared.Kernel;

using AresNexus.Tests.Integration.Infrastructure;

namespace AresNexus.Tests.Integration;

public class TransactionalOutboxChaosTests : IntegrationTestBase
{
    public TransactionalOutboxChaosTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SaveAsync_WhenDatabaseDisconnectionDuringOutboxSave_ShouldRollback()
    {
        // Arrange
        var mockSession = new Mock<IDocumentSession>();
        var mockEventStore = new Mock<IEventStore>();
        var mockEncryptionService = new Mock<IEncryptionService>();
        var mockEvents = new Mock<IEventStoreOperations>();
        
        mockSession.Setup(s => s.Events).Returns(mockEvents.Object);
        
        var account = new Account(Guid.NewGuid(), "Chaos Test");
        account.Deposit(new Money(100));

        // Simulate a database disconnection by throwing an exception during SaveChangesAsync
        mockSession.Setup(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database disconnection simulated."));

        var mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        mockConfiguration.Setup(c => c.GetSection(It.IsAny<string>())).Returns(new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>().Object);

        var mockResiliencePolicyFactory = new Mock<AresNexus.Settlement.Infrastructure.Resilience.IResiliencePolicyFactory>();
        
        mockResiliencePolicyFactory.Setup(f => f.GetDatabasePolicy())
            .Returns(Polly.Policy.NoOpAsync());

        var repository = new MartenAccountRepository(
            mockSession.Object, 
            mockEventStore.Object, 
            mockEncryptionService.Object, 
            mockConfiguration.Object, 
            mockResiliencePolicyFactory.Object);

        // Act
        var act = () => repository.SaveAsync(account, Enumerable.Empty<object>());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        
        // In a real scenario, Marten's session wouldn't have committed.
        // We verify that SaveChangesAsync was indeed called but failed.
        mockSession.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        
        // The aggregate should still have its uncommitted changes if we didn't call MarkChangesAsCommitted (which is at the end of SaveAsync)
        // However, the current implementation of SaveAsync calls MarkChangesAsCommitted AFTER SaveChangesAsync.
        // So if SaveChangesAsync fails, MarkChangesAsCommitted is NOT called.
        account.GetUncommittedChanges().Should().NotBeEmpty();
    }
}
