namespace AresNexus.Settlement.Infrastructure.Messaging;

/// <summary>
/// Represents a message to be sent via the outbox.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Message type.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Serialized content.</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>Creation timestamp.</summary>
    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Processing timestamp.</summary>
    public DateTime? ProcessedOnUtc { get; set; }
    /// <summary>Error message if any.</summary>
    public string? Error { get; set; }
}
