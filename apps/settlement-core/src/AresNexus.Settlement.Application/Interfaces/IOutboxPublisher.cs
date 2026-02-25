namespace AresNexus.Settlement.Application.Interfaces;

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
    Task PublishAsync(string topic, object payload);
}
