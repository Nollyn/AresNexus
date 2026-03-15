using AresNexus.Services.Settlement.Infrastructure.Messaging;
using AutoFixture;
using FluentAssertions;

namespace AresNexus.Tests.Unit.Infrastructure;

public class OutboxMessageTests
{
    private readonly IFixture _fixture = TestFixture.Create();

    [Fact]
    public void Should_Create_OutboxMessage_With_AutoFixture()
    {
        // Act
        var message = _fixture.Create<OutboxMessage>();

        // Assert
        message.Should().NotBeNull();
        message.Id.Should().NotBeEmpty();
        message.Type.Should().NotBeNullOrWhiteSpace();
        message.Content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Should_Set_And_Get_Properties()
    {
        // Arrange
        var message = new OutboxMessage();
        var id = Guid.NewGuid();
        var date = DateTime.UtcNow;
        var type = "Event.Type";
        var content = "{\"Key\":\"Value\"}";
        var traceId = "Trace1";
        var correlationId = "Corr1";
        var processedDate = DateTime.UtcNow.AddMinutes(1);
        var error = "Some error";
        var attempts = 3;
        var lastAttempt = DateTime.UtcNow.AddSeconds(30);

        // Act
        message.Id = id;
        message.OccurredOnUtc = date;
        message.Type = type;
        message.Content = content;
        message.TraceId = traceId;
        message.CorrelationId = correlationId;
        message.ProcessedOnUtc = processedDate;
        message.Error = error;
        message.AttemptCount = attempts;
        message.LastAttemptUtc = lastAttempt;
        message.IsPoison = true;

        // Assert
        message.Id.Should().Be(id);
        message.OccurredOnUtc.Should().Be(date);
        message.Type.Should().Be(type);
        message.Content.Should().Be(content);
        message.TraceId.Should().Be(traceId);
        message.CorrelationId.Should().Be(correlationId);
        message.ProcessedOnUtc.Should().Be(processedDate);
        message.Error.Should().Be(error);
        message.AttemptCount.Should().Be(attempts);
        message.LastAttemptUtc.Should().Be(lastAttempt);
        message.IsPoison.Should().BeTrue();
    }
}
