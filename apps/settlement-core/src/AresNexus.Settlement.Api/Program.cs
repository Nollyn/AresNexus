using System.Diagnostics.Metrics;
using System.Reflection;
using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Infrastructure.EventStore;
using AresNexus.Settlement.Infrastructure.Idempotency;
using AresNexus.Settlement.Infrastructure.Messaging;
using AresNexus.Settlement.Infrastructure.Repositories;
using AresNexus.Settlement.Infrastructure.Security;
using Asp.Versioning;
using FluentValidation;
using JasperFx;
using Marten;
using MediatR;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Custom Metrics
var meter = new Meter("AresNexus.Settlement", "1.0.0");

// Add metrics to DI for use in handlers/controllers
builder.Services.AddSingleton(meter);

// Options configuration with validation
builder.Services.AddOptions<ServiceBusOptions>()
    .Bind(builder.Configuration.GetSection("ServiceBus"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "ServiceBus:ConnectionString is required")
    .ValidateOnStart();

// Health checks
builder.Services.AddHealthChecks();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("AresNexus.Settlement.Api"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddSource("AresNexus.Settlement")
                       .AddConsoleExporter())
    .WithMetrics(m => m.AddAspNetCoreInstrumentation()
                       .AddRuntimeInstrumentation()
                       .AddProcessInstrumentation()
                       .AddMeter("AresNexus.Settlement")
                       .AddPrometheusExporter());

// MediatR + Validators
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.Load("AresNexus.Settlement.Application")));
builder.Services.AddValidatorsFromAssembly(Assembly.Load("AresNexus.Settlement.Application"));

// Infrastructure adapters
builder.Services.AddMarten(options =>
{
    options.Connection(builder.Configuration.GetConnectionString("PostgreSQL") ?? "Host=localhost;Database=AresNexus;Username=postgres;Password=postgres");
    options.AutoCreateSchemaObjects = AutoCreate.All;
}).UseLightweightSessions();

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandIdempotencyBehavior<,>));

builder.Services.AddScoped<IEventStore, MartenEventStore>();
builder.Services.AddScoped<IAccountRepository, MartenAccountRepository>();

// Redis-based Idempotency Store
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "AresNexus:";
});
builder.Services.AddSingleton<IIdempotencyStore, RedisIdempotencyStore>();

builder.Services.AddSingleton<IEncryptionService, PiiEncryptionService>();
builder.Services.AddSingleton<IOutboxPublisher, ServiceBusOutboxPublisher>();
builder.Services.AddHostedService<OutboxProcessor>();

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Global Exception Handling
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapGet("/health/live", () => Results.Ok(new { status = "LIVE" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "READY" }));

var versionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

app.MapPost("/api/v{version:apiVersion}/transactions", async (HttpContext context, ProcessTransactionCommand cmd, IValidator<ProcessTransactionCommand> validator, ISender mediator) =>
{
    // Extract Idempotency-Key from header if present
    if (context.Request.Headers.TryGetValue("Idempotency-Key", out var headerKey) && Guid.TryParse(headerKey, out var idempotencyGuid))
    {
        cmd = cmd with { IdempotencyKey = idempotencyGuid };
    }

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
    return ok ? Results.Accepted($"/api/v1/transactions/{cmd.AccountId}") : Results.BadRequest();
})
.WithApiVersionSet(versionSet)
.MapToApiVersion(1.0);

app.Run();

/// <summary>
/// Global exception handling middleware.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GlobalExceptionHandlingMiddleware"/> class.
/// </remarks>
internal sealed class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
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
