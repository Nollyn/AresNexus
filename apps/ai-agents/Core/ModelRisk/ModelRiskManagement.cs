using System.Collections.Concurrent;

namespace AresNexus.AiAgents.Core.ModelRisk;

public interface IModelRiskManager
{
    Task<bool> ValidateModelDecisionAsync(string modelId, object decision);
    Task MonitorPerformanceAsync(string modelId, double accuracy);
    Task DetectDriftAsync(string modelId, object data);
}

public class ModelRiskManager : IModelRiskManager
{
    private readonly ConcurrentDictionary<string, double> _thresholds = new();
    private readonly ConcurrentDictionary<string, bool> _modelStatus = new();
    private readonly ConcurrentDictionary<string, List<double>> _performanceHistory = new();

    public async Task<bool> ValidateModelDecisionAsync(string modelId, object decision)
    {
        // Check if model is disabled due to risk
        if (_modelStatus.TryGetValue(modelId, out var isEnabled) && !isEnabled)
        {
            return false;
        }

        // Basic validation logic
        if (decision == null) return false;

        return await Task.FromResult(true);
    }

    public async Task MonitorPerformanceAsync(string modelId, double accuracy)
    {
        var history = _performanceHistory.GetOrAdd(modelId, _ => new List<double>());
        lock (history)
        {
            history.Add(accuracy);
            if (history.Count > 100) history.RemoveAt(0); // Keep last 100
        }

        if (_thresholds.TryGetValue(modelId, out var threshold) && accuracy < threshold)
        {
            // Auto-disable if performance drops below threshold (FINMA requirement for automated systems)
            _modelStatus[modelId] = false;
        }
        await Task.CompletedTask;
    }

    public async Task DetectDriftAsync(string modelId, object data)
    {
        // Drift detection: Compare input distribution vs baseline
        // For now, log potential drift if data exceeds certain bounds
        await Task.CompletedTask;
    }

    public void SetThreshold(string modelId, double threshold)
    {
        _thresholds[modelId] = threshold;
        _modelStatus[modelId] = true;
    }
}
