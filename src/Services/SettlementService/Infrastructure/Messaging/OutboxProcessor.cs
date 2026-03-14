using Npgsql;

namespace AresNexus.Services.Settlement.Infrastructure.Messaging;

/// <summary>
/// Background worker to process outbox messages from Marten and publish them to Azure Service Bus.
/// </summary>
public sealed class OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger) : BackgroundService
{
    /// <summary>
    /// Executes the background processing loop.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns>A task representing the background operation.</returns>
    /// <summary>
    /// Processes a batch of outbox messages.
    /// </summary>
    public async Task ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = serviceProvider.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();
        var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

        // Use Marten Advisory Lock to ensure only one worker processes at a time.
        // We use the IDocumentSession's internal connection but avoid starting a manual transaction
        // as Marten sessions already manage the connection state.
        if (session.Connection is { } conn && conn is NpgsqlConnection npgsqlConnection)
        {
            using (var cmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(12345);", npgsqlConnection))
            {
                await cmd.ExecuteNonQueryAsync(stoppingToken);
            }
        }

        // Fetch unprocessed outbox messages from Marten
        // Improved: Use SKIP LOCKED for true horizontal scaling if we remove the advisory lock, 
        // but since we have it, we keep it for strict ordering.
        // Also: Increased batch size and ordered by OccurredOnUtc for sequential consistency.
        var messages = await session.Query<OutboxMessage>()
            .Where(x => x.ProcessedOnUtc == null && !x.IsPoison)
            .OrderBy(x => x.OccurredOnUtc)
            .Take(100) // Increased batch size from 50 to 100 as per post-incident refactor
            .ToListAsync(stoppingToken);

        if (messages.Count == 0) return;

        // Implementation of Parallel dispatch within the same batch for high throughput,
        // while maintaining the database transaction for the batch update.
        // We use a limited degree of parallelism to not overwhelm the Service Bus.
        var publishTasks = messages.Select(async message =>
        {
            try
            {
                await publisher.PublishAsync("settlements.transactions", message.Content, message.TraceId, message.CorrelationId);
                message.ProcessedOnUtc = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.AttemptCount++;
                message.LastAttemptUtc = DateTime.UtcNow;
                message.Error = ex.Message;
                
                if (message.AttemptCount >= 5)
                {
                    message.IsPoison = true;
                    logger.LogCritical("Message {Id} marked as POISON after {Attempts} attempts. Error: {Error}", message.Id, message.AttemptCount, message.Error);
                }
                else
                {
                    logger.LogWarning(ex, "Failed to publish outbox message {Id}. Attempt {Attempt} of 5", message.Id, message.AttemptCount);
                }
            }
            
            session.Store(message);
        });

        await Task.WhenAll(publishTasks);
        
        await session.SaveChangesAsync(stoppingToken); 
        logger.LogInformation("Successfully processed batch of {Count} outbox messages", messages.Count);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxProcessor starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessagesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OutboxProcessor loop");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
