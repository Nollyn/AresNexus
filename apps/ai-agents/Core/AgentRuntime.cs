using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AresNexus.AiAgents.Core;

public class AgentRuntime : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentRuntime> _logger;
    private readonly IEnumerable<IAgent> _agents;

    public AgentRuntime(
        IServiceProvider serviceProvider,
        ILogger<AgentRuntime> logger,
        IEnumerable<IAgent> agents)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _agents = agents;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent Runtime starting with {AgentCount} agents", _agents.Count());

        foreach (var agent in _agents)
        {
            _logger.LogInformation("Loaded agent: {AgentName} - {AgentDescription}", agent.Name, agent.Description);
        }

        // The runtime will listen to events and dispatch them to agents.
        // For now, we wait for the stopping token.
        // Actual event subscription will be handled by Infrastructure/EventSubscribers.

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
