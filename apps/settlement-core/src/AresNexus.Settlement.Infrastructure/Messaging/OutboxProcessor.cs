using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Infrastructure.EventStore;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AresNexus.Settlement.Infrastructure.Messaging;

/// <summary>
/// Background worker to process outbox messages from Marten and publish them to Azure Service Bus.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OutboxProcessor"/> class.
/// </remarks>
/// <param name="serviceProvider">The service provider to create scopes.</param>
/// <param name="logger">The logger for diagnostics.</param>
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

                // Fetch unprocessed outbox messages from Marten
                var messages = await session.Query<OutboxMessage>()
                    .Where(x => x.ProcessedOnUtc == null)
                    .OrderBy(x => x.OccurredOnUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (messages.Count == 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                foreach (var message in messages)
                {
                    var retryCount = 0;
                    var success = false;
                    const int maxRetries = 5;

                    while (!success && retryCount < maxRetries && !stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            // Publish to Azure Service Bus with Trace/Correlation IDs for Zero-Lag Observability requirement #2
                            await publisher.PublishAsync("settlements.transactions", message.Content, message.TraceId, message.CorrelationId);
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
                            
                            if (retryCount < maxRetries)
                            {
                                // Exponential backoff for financial reliability (Background worker requirement #2)
                                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                                await Task.Delay(delay, stoppingToken);
                            }
                            else
                            {
                                // Permanent failure for this attempt, save error state
                                session.Store(message);
                                await session.SaveChangesAsync(stoppingToken);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OutboxProcessor loop");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
