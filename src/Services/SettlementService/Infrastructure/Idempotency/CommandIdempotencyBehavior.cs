using AresNexus.Services.Settlement.Application.Commands;
using AresNexus.Services.Settlement.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AresNexus.Services.Settlement.Infrastructure.Idempotency;

/// <summary>
/// MediatR pipeline behavior to handle idempotency for commands.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="CommandIdempotencyBehavior{TRequest, TResponse}"/> class.
/// </remarks>
public sealed class CommandIdempotencyBehavior<TRequest, TResponse>(
    IIdempotencyStore idempotencyStore,
    ILogger<CommandIdempotencyBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ProcessTransactionCommand command)
        {
            return await next();
        }

        if (await idempotencyStore.ExistsAsync(command.IdempotencyKey))
        {
            logger.LogInformation("Command with IdempotencyKey {IdempotencyKey} already processed. Returning cached result.", command.IdempotencyKey);
            var cachedResult = await idempotencyStore.GetAsync<TResponse>(command.IdempotencyKey);
            return cachedResult!;
        }

        var response = await next();

        await idempotencyStore.StoreAsync(command.IdempotencyKey, response!);
        logger.LogInformation("Command with IdempotencyKey {IdempotencyKey} processed and cached.", command.IdempotencyKey);

        return response;
    }
}
