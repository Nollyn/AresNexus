using Microsoft.SemanticKernel;

namespace AresNexus.AiAgents.Core;

public interface IDecisionEngine
{
    Task<string> ReasonAsync(string prompt, KernelArguments? args = null);
}

public class DecisionEngine : IDecisionEngine
{
    private readonly Kernel _kernel;

    public DecisionEngine(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> ReasonAsync(string prompt, KernelArguments? args = null)
    {
        var result = await _kernel.InvokePromptAsync(prompt, args);
        return result.ToString();
    }
}
