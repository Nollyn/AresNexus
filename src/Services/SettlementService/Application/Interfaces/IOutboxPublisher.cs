namespace AresNexus.Services.Settlement.Application.Interfaces;

/// <summary>
/// Publishes integration events/messages as part of the outbox pattern.
/// </summary>
public interface IOutboxPublisher
{
    /// <summary>
    /// Publishes a message payload to a topic.
    /// </summary>
    /// <param name="topic">The destination topic or queue.</param>
    /// <param name="payload">The message payload.</param>
    /// <param name="traceId">The optional trace identifier.</param>
    /// <param name="correlationId">The optional correlation identifier.</param>
    Task PublishAsync(string topic, object payload, string? traceId = null, string? correlationId = null);
}
