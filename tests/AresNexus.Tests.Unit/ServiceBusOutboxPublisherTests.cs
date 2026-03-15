using AresNexus.Services.Settlement.Application.Interfaces;
using AresNexus.Services.Settlement.Infrastructure.Messaging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AresNexus.Tests.Unit;

public class ServiceBusOutboxPublisherTests
{
    private readonly Mock<ILogger<ServiceBusOutboxPublisher>> _loggerMock = new();
    private readonly Mock<IOptions<ServiceBusOptions>> _optionsMock = new();
    private readonly ServiceBusOptions _options = new();

    public ServiceBusOutboxPublisherTests()
    {
        _optionsMock.Setup(x => x.Value).Returns(_options);
    }

    [Fact]
    public async Task PublishAsync_ShouldLogWarningAndReturn_WhenMockConnectionString()
    {
        // Arrange
        _options.ConnectionString = "Endpoint=sb://mock.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=mock";
        var sut = new ServiceBusOutboxPublisher(_loggerMock.Object, _optionsMock.Object);

        // Act
        await sut.PublishAsync("topic", new { Data = "test" });

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MOCK MODE")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldThrow_WhenInvalidConnectionString()
    {
        // Arrange
        _options.ConnectionString = "Invalid Connection String";
        var sut = new ServiceBusOutboxPublisher(_loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        await sut.Awaiting(x => x.PublishAsync("topic", new { Data = "test" }))
            .Should().ThrowAsync<Exception>();
        
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to publish")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_ShouldNotThrow_WhenClientIsNull()
    {
        // Arrange
        var sut = new ServiceBusOutboxPublisher(_loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        await sut.Awaiting(x => x.DisposeAsync().AsTask()).Should().NotThrowAsync();
    }
}
