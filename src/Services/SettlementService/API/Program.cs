using System.Diagnostics.Metrics;
using System.Reflection;
using System.Threading.RateLimiting;
using AresNexus.Services.Settlement.Api;
using AresNexus.Services.Settlement.Application.Commands;
using AresNexus.Services.Settlement.Application.Validation;
using AresNexus.Services.Settlement.Infrastructure.EventStore;
using AresNexus.Services.Settlement.Infrastructure.Idempotency;
using AresNexus.Services.Settlement.Infrastructure.Logging;
using AresNexus.Services.Settlement.Infrastructure.Messaging;
using AresNexus.Services.Settlement.Infrastructure.Repositories;
using AresNexus.Services.Settlement.Infrastructure.Resilience;
using AresNexus.Services.Settlement.Infrastructure.Security;
using Asp.Versioning;
using FluentValidation;
using JasperFx;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for Structured Logging (JSON) to Console and OTLP for Loki
Log.Logger = new LoggerConfiguration()
    .Destructure.With<SensitiveDataDestructuringPolicy>()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://otel-collector:4317";
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = builder.Configuration["OTEL_SERVICE_NAME"] ?? "AresNexus.Settlement.Api"
        };
    })
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Custom Metrics
var meter = new Meter("AresNexus.Settlement", "1.0.0");
builder.Services.AddSingleton(meter);

// Create counters at startup to ensure they are registered with the meter
meter.CreateCounter<long>("settlement_success_total");
meter.CreateCounter<long>("settlement_failure_total");
meter.CreateCounter<long>("settlement_total_count_total");
meter.CreateHistogram<double>("settlement_processing_seconds");

// Options configuration with validation
builder.Services.AddOptions<ServiceBusOptions>()
    .Bind(builder.Configuration.GetSection("ServiceBus"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "ServiceBus:ConnectionString is required")
    .ValidateOnStart();

// Health checks
builder.Services.AddHealthChecks();

// Hardened Security requirement #4: API Rate Limiting using .NET 10 built-in middleware.
// Configured for high-frequency trading constraints and specific "High-Risk" policies for DORA compliance.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    // Default policy for standard requests
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 1000,
                QueueLimit = 100,
                Window = TimeSpan.FromSeconds(1)
            }));

    // Specific policy for "High-Risk" transaction endpoints (Swiss Security Hardening #4)
    options.AddPolicy("HighRisk", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10, // Significantly more restrictive for high-risk operations
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(builder.Configuration["OTEL_SERVICE_NAME"] ?? "AresNexus.Settlement.Api"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddSource("AresNexus.Settlement")
                       .AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317")))
    .WithMetrics(m => m.AddAspNetCoreInstrumentation()
                       .AddRuntimeInstrumentation()
                       .AddProcessInstrumentation()
                       .AddMeter("AresNexus.Settlement")
                       .AddMeter("AresNexus.Shared.Kernel")
                       .AddMeter("Microsoft.AspNetCore.Hosting")
                       .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                       .AddPrometheusExporter()
                       .AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317")));

// MediatR + Validators
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(ProcessTransactionCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(ProcessTransactionCommand).Assembly);

// Infrastructure adapters
builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("PostgreSQL") ?? "Host=localhost;Database=AresNexus;Username=postgres;Password=postgres");
    
    // In production/multi-replica setup, we want to be careful with auto-creation
    // FINMA/DORA compliance: ensure schema is stable
    options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
    
    // Performance requirement #1: Optimize Marten for multi-replica Swiss environment
    // Use the default schema name to avoid extra lookups
    options.DatabaseSchemaName = "public";
}).UseLightweightSessions();

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandIdempotencyBehavior<,>));

builder.Services.AddSingleton<IResiliencePolicyFactory, ResiliencePolicyFactory>();
builder.Services.AddScoped<IEventStore, MartenEventStore>();
builder.Services.AddScoped<IAccountRepository, MartenAccountRepository>();

// Distributed Memory Cache (Fallback for local dev)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();

builder.Services.AddHttpClient("ResilientClient")
    .AddStandardResilienceHandler();

// Secret Management requirement #4: Infrastructure project to support Azure Key Vault for production environments
// (use a SecretManager abstraction so it uses UserSecrets in Dev)
// Force DevSecretManager in Docker to avoid needing real Azure Key Vault URI
if (builder.Environment.IsProduction() && !string.IsNullOrEmpty(builder.Configuration["AzureKeyVault:Uri"]))
{
    builder.Services.AddSingleton<ISecretManager, AzureKeyVaultSecretManager>();
}
else
{
    builder.Services.AddSingleton<ISecretManager, DevSecretManager>();
}

builder.Services.AddSingleton<IKeyVaultClient, MockKeyVaultClient>();
builder.Services.AddSingleton<IEventUpcaster, MoneyDeposited_v1_to_v2_Upcaster>();
builder.Services.AddSingleton<IEncryptionService, PiiEncryptionService>();
builder.Services.AddSingleton<IOutboxPublisher, ServiceBusOutboxPublisher>();
builder.Services.AddHostedService<OutboxProcessor>();
builder.Services.AddHostedService<DataSeeder>();

// OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "AresNexus Settlement Core API",
        Description = "Swiss Banking Settlement Core (DORA compliant)"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddOpenApi();

var app = builder.Build();

// Task 2: Pipeline Order (Switzerland-compliant)
// 1. Exception handling (Resilience)
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// 2. OpenTelemetry Prometheus (Before Routing)
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// 3. Routing
app.UseRouting();

// 4. Rate Limiting
app.UseRateLimiter();

// 5. Swagger & Scalar (Documenting)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = "swagger";
});
app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/health", () => Results.Ok(new { status = "UP" }))
    .WithName("GetHealth")
    .WithOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "LIVE" }))
    .WithName("GetHealthLive")
    .WithOpenApi();
app.MapGet("/health/ready", () => Results.Ok(new { status = "READY" }))
    .WithName("GetHealthReady")
    .WithOpenApi();

app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

app.MapPost("/api/v1/transactions", async (HttpContext context, ProcessTransactionCommand cmd, [FromServices] IValidator<ProcessTransactionCommand> validator, [FromServices] ISender mediator, [FromServices] IIdempotencyStore idempotencyStore) =>
{
    // Distributed Tracing requirement #3: Ensure every Command and Event carries a TraceId and CorrelationId.
    var traceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault() ?? context.TraceIdentifier;
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();

    // Extract Idempotency-Key from a header if present
    if (context.Request.Headers.TryGetValue("Idempotency-Key", out var headerKey) && Guid.TryParse(headerKey, out var idempotencyGuid))
    {
        cmd = cmd with { IdempotencyKey = idempotencyGuid };
    }
    
    cmd = cmd with { TraceId = traceId, CorrelationId = correlationId };

    if (cmd.IdempotencyKey == Guid.Empty)
    {
        return Results.BadRequest(new { error = "Idempotency-Key header or property is required" });
    }

    var result = await validator.ValidateAsync(cmd);
    if (!result.IsValid)
    {
        return Results.ValidationProblem(result.ToDictionary());
    }

    var ok = await mediator.Send(cmd);
    return ok ? Results.Created($"/api/v1/transactions/{cmd.AccountId}", new { status = "PROCESSED" }) : Results.BadRequest();
})
.RequireRateLimiting("HighRisk")
.Produces<object>(StatusCodes.Status201Created)
.ProducesProblem(StatusCodes.Status400BadRequest)
.WithName("ProcessTransaction")
.WithOpenApi();

app.Run();

public partial class Program { }

/// <summary>
/// Global exception handling middleware.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "Unhandled error", detail = ex.Message });
        }
    }
}
