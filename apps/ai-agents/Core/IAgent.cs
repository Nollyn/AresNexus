using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace AresNexus.AiAgents.Core;

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    Task ProcessEventAsync(object @event, CancellationToken ct = default);
}

public abstract class BaseAgent : IAgent
{
    protected readonly Kernel Kernel;
    protected readonly ILogger Logger;

    protected BaseAgent(Kernel kernel, ILogger logger)
    {
        Kernel = kernel;
        Logger = logger;
    }

    public abstract string Name { get; }
    public abstract string Description { get; }

    public abstract Task ProcessEventAsync(object @event, CancellationToken ct = default);
}
