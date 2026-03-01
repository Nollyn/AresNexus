using Npgsql;

namespace AresNexus.Settlement.Infrastructure.Messaging;

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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxProcessor starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();
                var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

                // Use Marten Advisory Lock to ensure only one worker processes at a time
                await session.Connection.OpenAsync(stoppingToken);
                using var tx = await session.Connection.BeginTransactionAsync(stoppingToken);
                using (var cmd = new NpgsqlCommand("SELECT pg_advisory_xact_lock(12345);", session.Connection, tx))
                {
                    await cmd.ExecuteNonQueryAsync(stoppingToken);
                }

                // Fetch unprocessed outbox messages from Marten
                var messages = await session.Query<OutboxMessage>()
                    .Where(x => x.ProcessedOnUtc == null)
                    .OrderBy(x => x.OccurredOnUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (messages.Count == 0)
                {
                    await tx.CommitAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                foreach (var message in messages)
                {
                    // Publish to Azure Service Bus with Trace/Correlation IDs for Zero-Lag Observability requirement #2
                    await publisher.PublishAsync("settlements.transactions", message.Content, message.TraceId, message.CorrelationId);
                    message.ProcessedOnUtc = DateTime.UtcNow;
                    
                    // Mark as processed in the database
                    session.Store(message);
                }
                
                await session.SaveChangesAsync(stoppingToken); 
                await tx.CommitAsync(stoppingToken); // releases the transaction-level advisory lock
                logger.LogInformation("Successfully processed batch of {Count} outbox messages", messages.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OutboxProcessor loop");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
