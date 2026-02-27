using AresNexus.Settlement.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace AresNexus.Settlement.Infrastructure.Idempotency;

/// <summary>
/// Redis-based implementation of <see cref="IIdempotencyStore"/> using <see cref="IDistributedCache"/> for Zurich Financial Safety.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RedisIdempotencyStore"/> class.
/// This ensures that the same command is not processed twice across multiple service instances.
/// </remarks>
/// <param name="cache">The distributed cache instance.</param>
public sealed class RedisIdempotencyStore(IDistributedCache cache) : IIdempotencyStore
{
    private static readonly DistributedCacheEntryOptions Options = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
    };

    /// <summary>
    /// Checks if an idempotency key exists in the Redis cache.
    /// </summary>
    /// <param name="key">The unique idempotency key.</param>
    /// <returns>True if the key exists; otherwise, false.</returns>
    public async Task<bool> ExistsAsync(Guid key)
    {
        var data = await cache.GetAsync(key.ToString());
        return data != null;
    }

    /// <summary>
    /// Stores the result of a command execution in the Redis cache.
    /// </summary>
    /// <param name="key">The unique idempotency key.</param>
    /// <param name="result">The result to cache.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StoreAsync(Guid key, object result)
    {
        var json = JsonSerializer.Serialize(result);
        await cache.SetStringAsync(key.ToString(), json, Options);
    }

    /// <summary>
    /// Retrieves the cached result for a given idempotency key.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="key">The unique idempotency key.</param>
    /// <returns>The cached result or null if not found.</returns>
    public async Task<T?> GetAsync<T>(Guid key)
    {
        var json = await cache.GetStringAsync(key.ToString());
        return json == null ? default : JsonSerializer.Deserialize<T>(json);
    }
}
