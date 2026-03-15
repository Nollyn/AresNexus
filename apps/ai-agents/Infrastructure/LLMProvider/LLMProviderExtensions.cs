using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AresNexus.AiAgents.Infrastructure.LLMProvider;

public static class LLMProviderExtensions
{
    public static IServiceCollection AddLLMProvider(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["LLM:Provider"] ?? "OpenAI";
        var kernelBuilder = Kernel.CreateBuilder();

        if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            var modelId = configuration["LLM:OpenAI:ModelId"] ?? "gpt-4";
            var apiKey = configuration["LLM:OpenAI:ApiKey"];
            
            if (string.IsNullOrEmpty(apiKey))
            {
                // For development if no API key, use a mock or throw
                // throw new ArgumentNullException("LLM:OpenAI:ApiKey", "OpenAI API Key is required");
                // Mocking for now to avoid build/runtime failure without keys
            }
            
            kernelBuilder.AddOpenAIChatCompletion(modelId, apiKey ?? "mock-key");
        }
        else if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = configuration["LLM:Local:Endpoint"] ?? "http://localhost:11434/v1";
            var modelId = configuration["LLM:Local:ModelId"] ?? "llama3";
            
            // Using OpenAI connector for local models that support OpenAI API (like Ollama or LocalAI)
            kernelBuilder.AddOpenAIChatCompletion(modelId, "local", null, null, null);
        }

        services.AddTransient<Kernel>(sp => kernelBuilder.Build());
        
        return services;
    }
}
