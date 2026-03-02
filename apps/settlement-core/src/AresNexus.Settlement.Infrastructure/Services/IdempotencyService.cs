using AresNexus.Settlement.Application.Interfaces;

namespace AresNexus.Settlement.Infrastructure.Services;

public sealed class IdempotencyService(IIdempotencyStore store) : IIdempotencyService
{
    public async Task<bool> HasBeenProcessedAsync(Guid key, CancellationToken cancellationToken = default)
    {
        return await store.ExistsAsync(key);
    }

    public async Task MarkAsProcessedAsync(Guid key, object result, CancellationToken cancellationToken = default)
    {
        await store.StoreAsync(key, result);
    }
}
