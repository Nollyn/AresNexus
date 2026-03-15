using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace AresNexus.AiAgents.Infrastructure.MetricsCollectors;

public class AgentMetrics
{
    private readonly Counter<long> _decisionsTotal;
    private readonly Counter<long> _anomaliesDetected;
    private readonly Counter<long> _actionsTriggered;

    public AgentMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("AresNexus.AiAgents");
        
        _decisionsTotal = meter.CreateCounter<long>("agent_decisions_total", "Number of agent decisions");
        _anomaliesDetected = meter.CreateCounter<long>("agent_anomalies_detected", "Number of anomalies detected by agents");
        _actionsTriggered = meter.CreateCounter<long>("agent_actions_triggered", "Number of actions triggered by agents");
    }

    public void RecordDecision(string agentName)
    {
        _decisionsTotal.Add(1, new TagList { { "agent", agentName } });
    }

    public void RecordAnomaly(string agentName, string anomalyType)
    {
        _anomaliesDetected.Add(1, new TagList { { "agent", agentName }, { "type", anomalyType } });
    }

    public void RecordAction(string agentName, string actionName)
    {
        _actionsTriggered.Add(1, new TagList { { "agent", agentName }, { "action", actionName } });
    }
}
