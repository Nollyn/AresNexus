using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Infrastructure.EventStore;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AresNexus.Settlement.Infrastructure.Messaging;

/// <summary>
/// Background worker to process outbox messages from Marten.
/// </summary>
public sealed class OutboxProcessor(IServiceProvider serviceProvider, ILogger<OutboxProcessor> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IOutboxPublisher>();
                var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();

                // Fetch unprocessed outbox messages from Marten
                var messages = await session.Query<OutboxMessage>()
                    .Where(x => x.ProcessedOnUtc == null)
                    .OrderBy(x => x.OccurredOnUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
                {
                    var retryCount = 0;
                    var success = false;

                    while (!success && retryCount < 5 && !stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            await publisher.PublishAsync("settlements.transactions", message.Content);
                            message.ProcessedOnUtc = DateTime.UtcNow;
                            
                            // Mark as processed in the database
                            session.Store(message);
                            await session.SaveChangesAsync(stoppingToken);
                            
                            success = true;
                            logger.LogInformation("Successfully processed outbox message {MessageId}", message.Id);
                        }
                        catch (Exception ex)
                        {
                            retryCount++;
                            logger.LogError(ex, "Error processing outbox message {MessageId}. Retry {RetryCount}", message.Id, retryCount);
                            message.Error = ex.Message;
                            
                            if (retryCount < 5)
                            {
                                // Exponential backoff for financial reliability
                                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                                await Task.Delay(delay, stoppingToken);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OutboxProcessor loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
