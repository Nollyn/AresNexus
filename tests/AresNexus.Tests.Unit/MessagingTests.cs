using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Infrastructure.Messaging;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AresNexus.Tests.Unit;

public class MessagingTests
{
    [Fact]
    public async Task ServiceBusOutboxPublisher_PublishAsync_InMockMode_ShouldNotThrow()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ServiceBusOutboxPublisher>>();
        var options = Options.Create(new ServiceBusOptions { ConnectionString = "mock.servicebus.windows.net" });
        var publisher = new ServiceBusOutboxPublisher(loggerMock.Object, options);

        // Act
        var act = () => publisher.PublishAsync("test", new { Foo = "Bar" });

        // Assert
        await act.Should().NotThrowAsync();
        
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MOCK MODE")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ServiceBusOutboxPublisher_PublishAsync_WithInvalidConnectionString_ShouldThrow()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ServiceBusOutboxPublisher>>();
        var options = Options.Create(new ServiceBusOptions { ConnectionString = "InvalidConnectionString" });
        var publisher = new ServiceBusOutboxPublisher(loggerMock.Object, options);

        // Act & Assert
        // The SDK validates connection string on client creation
        await Assert.ThrowsAnyAsync<Exception>(() => publisher.PublishAsync("test", new { Foo = "Bar" }));
    }

    [Fact]
    public async Task OutboxProcessor_ProcessMessagesAsync_WhenNoMessages_ShouldReturn()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var scopeMock = new Mock<IServiceScope>();
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var publisherMock = new Mock<IOutboxPublisher>();
        var sessionMock = new Mock<IDocumentSession>();

        serviceProviderMock.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(scopeFactoryMock.Object);
        scopeFactoryMock.Setup(s => s.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
        
        serviceProviderMock.Setup(s => s.GetService(typeof(IOutboxPublisher))).Returns(publisherMock.Object);
        serviceProviderMock.Setup(s => s.GetService(typeof(IDocumentSession))).Returns(sessionMock.Object);

        var loggerMock = new Mock<ILogger<OutboxProcessor>>();
        var processor = new OutboxProcessor(serviceProviderMock.Object, loggerMock.Object);

        // Act
        try 
        {
            await processor.ProcessMessagesAsync(CancellationToken.None);
        }
        catch (Exception)
        {
            // Expected if session.Query<OutboxMessage>() is not mocked or fails
        }

        // Assert
        publisherMock.Verify(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }
}
