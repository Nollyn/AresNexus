using AresNexus.AiAgents.Core;
using AresNexus.AiAgents.Agents.FraudAgent;
using AresNexus.AiAgents.Agents.ComplianceAgent;
using AresNexus.AiAgents.Agents.RiskAgent;
using AresNexus.AiAgents.Agents.OpsAgent;
using AresNexus.AiAgents.Agents.SettlementAgent;
using AresNexus.AiAgents.Agents.ObservabilityAgent;
using AresNexus.AiAgents.Core.Governance;
using AresNexus.AiAgents.Core.Protection;
using AresNexus.AiAgents.Core.ModelRisk;
using AresNexus.AiAgents.Core.DecisionGate;
using AresNexus.AiAgents.Infrastructure.LLMProvider;
using AresNexus.AiAgents.Infrastructure.MetricsCollectors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Data Protection Layer
builder.Services.AddSingleton<IDataProtectionGateway, DataProtectionGateway>();

// AI Governance Layer
builder.Services.AddSingleton<IAIModelRegistry, AIModelRegistry>();
builder.Services.AddSingleton<IAgentAuditLogger, AgentAuditLogger>();

// Model Risk Management
builder.Services.AddSingleton<IModelRiskManager, ModelRiskManager>();

// Decision Gate
builder.Services.AddSingleton<IDecisionGate, DecisionGate>();

// Agents
builder.Services.AddSingleton<IAgent, FraudAgent>();
builder.Services.AddSingleton<IAgent, ComplianceAgent>();
builder.Services.AddSingleton<IAgent, RiskAgent>();
builder.Services.AddSingleton<IAgent, OpsAgent>();
builder.Services.AddSingleton<IAgent, SettlementAgent>();
builder.Services.AddSingleton<IAgent, ObservabilityAgent>();

// Core
builder.Services.AddSingleton<IDecisionEngine, DecisionEngine>();
builder.Services.AddHostedService<AgentRuntime>();

// Infrastructure
builder.Services.AddLLMProvider(builder.Configuration);
builder.Services.AddSingleton<AgentMetrics>();
builder.Services.AddMetrics();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithMetrics(m => m
        .AddMeter("AresNexus.AiAgents")
        .AddRuntimeInstrumentation()
        .AddPrometheusExporter());

var host = builder.Build();

await host.RunAsync();
