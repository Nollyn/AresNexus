using System.Reflection;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for Structured Logging (JSON) to Console and OTLP for Loki
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = "http://otel-collector:4317";
        options.ResourceAttributes = new Dictionary<string, object>
        {
            ["service.name"] = "AresNexus.Gateway.Api"
        };
    })
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// OpenTelemetry configuration
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("AresNexus.Gateway.Api"))
    .WithTracing(t => t.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317")))
    .WithMetrics(m => m.AddAspNetCoreInstrumentation()
                       .AddRuntimeInstrumentation()
                       .AddProcessInstrumentation()
                       .AddPrometheusExporter()
                       .AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317")));

// Add services to the container.
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "AresNexus Gateway API",
        Description = "Swiss Banking API Gateway (DORA compliant)"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.AddOpenApi();

// Task 3: Ensure all HttpClient calls in the Gateway use Polly for retries and circuit breaking.
builder.Services.AddHttpClient("ComplianceBridge")
    .AddStandardResilienceHandler();

var app = builder.Build();

// Configure the HTTP request pipeline.
// 1. OpenTelemetry Prometheus (Before Routing)
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// 2. Swagger & Scalar (Documenting)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = "swagger";
});
app.MapOpenApi();

app.MapGet("/", () => "AresNexus Gateway API (Active)");

// Returns the health status of the Gateway API.
app.MapGet("/health", () => Results.Ok(new { status = "UP" }));

app.Run();
