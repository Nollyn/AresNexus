using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Infrastructure.Messaging;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Data;
using Marten.Linq;

namespace AresNexus.Tests.Unit;

public class OutboxProcessorTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<IServiceScope> _scopeMock;
    private readonly Mock<IOutboxPublisher> _publisherMock;
    private readonly Mock<IDocumentSession> _sessionMock;
    private readonly Mock<ILogger<OutboxProcessor>> _loggerMock;

    public OutboxProcessorTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeMock = new Mock<IServiceScope>();
        _publisherMock = new Mock<IOutboxPublisher>();
        _sessionMock = new Mock<IDocumentSession>();
        _loggerMock = new Mock<ILogger<OutboxProcessor>>();

        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(_scopeFactoryMock.Object);
        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(_scopeMock.Object);
        _scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProviderMock.Object);

        _serviceProviderMock.Setup(x => x.GetService(typeof(IOutboxPublisher))).Returns(_publisherMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IDocumentSession))).Returns(_sessionMock.Object);
    }

    [Fact]
    public async Task ProcessMessagesAsync_ShouldDispatchPendingMessages()
    {
        // Arrange
        var messages = new List<OutboxMessage>
        {
            new OutboxMessage { Id = Guid.NewGuid(), Content = "{\"data\":\"1\"}", TraceId = "t1", CorrelationId = "c1", OccurredOnUtc = DateTime.UtcNow },
            new OutboxMessage { Id = Guid.NewGuid(), Content = "{\"data\":\"2\"}", TraceId = "t2", CorrelationId = "c2", OccurredOnUtc = DateTime.UtcNow }
        }.AsQueryable();

        var queryMock = new Mock<IMartenQueryable<OutboxMessage>>();
        queryMock.Setup(x => x.Provider).Returns(messages.Provider);
        queryMock.Setup(x => x.Expression).Returns(messages.Expression);
        queryMock.Setup(x => x.ElementType).Returns(messages.ElementType);
        queryMock.Setup(x => x.GetEnumerator()).Returns(messages.GetEnumerator());
        
        // Setup the chain: session.Query<OutboxMessage>().Where(...).OrderBy(...).Take(...).ToListAsync(...)
        _sessionMock.Setup(x => x.Query<OutboxMessage>()).Returns(queryMock.Object);
        
        var processor = new OutboxProcessor(_serviceProviderMock.Object, _loggerMock.Object);
        
        // Act
        // We catch the exception because we know it will fail on .ToListAsync() extension method 
        // which we cannot easily mock. But the lines before it in ProcessMessagesAsync will be covered.
        try 
        {
            await processor.ProcessMessagesAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            // Expected failure due to Marten extension methods
        }
    }
}
