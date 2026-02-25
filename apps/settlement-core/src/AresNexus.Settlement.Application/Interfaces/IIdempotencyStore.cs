namespace AresNexus.Settlement.Application.Interfaces;

/// <summary>
/// Interface for an idempotency store.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Checks if a request with the given key has already been processed.
    /// </summary>
    Task<bool> ExistsAsync(Guid key);

    /// <summary>
    /// Stores the result of a processed request.
    /// </summary>
    Task StoreAsync(Guid key, object result);

    /// <summary>
    /// Gets the cached result for a request.
    /// </summary>
    Task<T?> GetAsync<T>(Guid key);
}
