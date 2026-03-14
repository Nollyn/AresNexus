using System.Collections.Concurrent;
using System.Text.Json;
using AresNexus.Services.Settlement.Application.Interfaces;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AresNexus.Services.Settlement.Infrastructure.Messaging;

/// <summary>
/// Options for configuring Service Bus publisher.
/// </summary>
public sealed class ServiceBusOptions
{
    /// <summary>
    /// Gets or sets the connection string for Azure Service Bus.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
}

/// <summary>
/// Outbox publisher using Azure Service Bus SDK.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ServiceBusOutboxPublisher"/> class.
/// </remarks>
public sealed class ServiceBusOutboxPublisher(ILogger<ServiceBusOutboxPublisher> logger, IOptions<ServiceBusOptions> options) : IOutboxPublisher, IAsyncDisposable
{
    private readonly ServiceBusOptions _options = options.Value;
    private ServiceBusClient? _client;
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    /// <inheritdoc />
    public async Task PublishAsync(string topic, object payload, string? traceId = null, string? correlationId = null)
    {
        if (_options.ConnectionString.Contains("mock.servicebus.windows.net"))
        {
            logger.LogWarning("[Outbox] MOCK MODE: Skipping actual Service Bus publishing to {Topic} due to mock connection string.", topic);
            return;
        }

        try
        {
            _client ??= new ServiceBusClient(_options.ConnectionString);
            var sender = _senders.GetOrAdd(topic, t => _client.CreateSender(t));

            var json = JsonSerializer.Serialize(payload);
            var message = new ServiceBusMessage(json)
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString()
            };

            // Propagate Trace and Correlation IDs for DORA Zero-Lag Observability requirement #2
            if (!string.IsNullOrEmpty(traceId))
            {
                message.ApplicationProperties["TraceId"] = traceId;
            }
            if (!string.IsNullOrEmpty(correlationId))
            {
                message.ApplicationProperties["CorrelationId"] = correlationId;
            }

            await sender.SendMessageAsync(message);
            logger.LogInformation("[Outbox] Successfully published to {Topic} with CorrelationId: {CorrelationId}", topic, correlationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Outbox] Failed to publish to {Topic}", topic);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }
        if (_client != null)
        {
            await _client.DisposeAsync();
        }
    }
}
