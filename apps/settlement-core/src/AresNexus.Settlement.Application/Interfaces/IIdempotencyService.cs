namespace AresNexus.Settlement.Application.Interfaces;

/// <summary>
/// Interface for Idempotency Service to prevent duplicate processing.
/// </summary>
public interface IIdempotencyService
{
    Task<bool> HasBeenProcessedAsync(Guid key, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid key, object result, CancellationToken cancellationToken = default);
}
