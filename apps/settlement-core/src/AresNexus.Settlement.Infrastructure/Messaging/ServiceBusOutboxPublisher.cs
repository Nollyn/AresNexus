using System.Text.Json;
using AresNexus.Settlement.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AresNexus.Settlement.Infrastructure.Messaging;

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
/// Outbox publisher using Azure Service Bus SDK patterns. In this scaffold it logs instead of sending.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ServiceBusOutboxPublisher"/> class.
/// </remarks>
public sealed class ServiceBusOutboxPublisher(ILogger<ServiceBusOutboxPublisher> logger, IOptions<ServiceBusOptions> options) : IOutboxPublisher
{
    private readonly ServiceBusOptions _options = options.Value;

    /// <inheritdoc />
    public Task PublishAsync(string topic, object payload)
    {
        // In production, we would create a ServiceBusClient using _options.ConnectionString and send the message.
        // For scaffold, we log the outgoing message as part of outbox responsibility.
        var json = JsonSerializer.Serialize(payload);
        logger.LogInformation("[Outbox] Publishing to {Topic}: {Payload}", topic, json);
        return Task.CompletedTask;
    }
}
