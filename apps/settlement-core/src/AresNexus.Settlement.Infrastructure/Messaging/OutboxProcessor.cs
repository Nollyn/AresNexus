using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Infrastructure.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AresNexus.Settlement.Infrastructure.Messaging;

/// <summary>
/// Background worker to process outbox messages.
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

                if (scope.ServiceProvider.GetRequiredService<IEventStore>() is InMemoryCosmosEventStore eventStore)
                {
                    var messages = eventStore.GetUnprocessedOutboxMessages();

                    foreach (var message in messages)
                    {
                        try
                        {
                            await publisher.PublishAsync("settlements.transactions", message.Content);
                            message.ProcessedOnUtc = DateTime.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);
                            message.Error = ex.Message;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OutboxProcessor");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
