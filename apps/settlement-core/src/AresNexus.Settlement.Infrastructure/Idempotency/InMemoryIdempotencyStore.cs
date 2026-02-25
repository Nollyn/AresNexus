using System.Collections.Concurrent;
using AresNexus.Settlement.Application.Interfaces;

namespace AresNexus.Settlement.Infrastructure.Idempotency;

/// <summary>
/// Mock implementation of IIdempotencyStore using Redis-like behavior.
/// In production, this would use StackExchange.Redis.
/// </summary>
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<Guid, object> _cache = new();

    /// <inheritdoc />
    public Task<bool> ExistsAsync(Guid key)
    {
        return Task.FromResult(_cache.ContainsKey(key));
    }

    /// <inheritdoc />
    public Task StoreAsync(Guid key, object result)
    {
        _cache[key] = result;
        // In a real Redis implementation, we would set an expiry of 24 hours here.
        // For this mock, we just store it.
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(Guid key)
    {
        return _cache.TryGetValue(key, out var result) ? Task.FromResult((T?)result) : Task.FromResult(default(T));
    }
}
