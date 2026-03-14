namespace AresNexus.Services.Settlement.Infrastructure.Messaging;

/// <summary>
/// Represents a message to be sent via the outbox.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Unique identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Message creation timestamp.</summary>
    public DateTime OccurredOnUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Message type.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Serialized content.</summary>
    public string Content { get; set; } = string.Empty;
    /// <summary>Trace identifier.</summary>
    public string? TraceId { get; set; }
    /// <summary>Correlation identifier.</summary>
    public string? CorrelationId { get; set; }
    /// <summary>Processing timestamp.</summary>
    public DateTime? ProcessedOnUtc { get; set; }
    /// <summary>Error message if any.</summary>
    public string? Error { get; set; }
    /// <summary>Number of processing attempts.</summary>
    public int AttemptCount { get; set; }
    /// <summary>Timestamp of last attempt.</summary>
    public DateTime? LastAttemptUtc { get; set; }
    /// <summary>Is the message considered poison.</summary>
    public bool IsPoison { get; set; }
}
