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
    .ConfigureResource(r => r
        .AddService("AresNexus.Gateway.Api")
        .AddAttributes(new Dictionary<string, object>
        {
            ["region"] = builder.Configuration["REGION"] ?? "switzerland-zurich"
        }))
    .WithTracing(t => t.AddAspNetCoreInstrumentation()
                       .AddHttpClientInstrumentation()
                       .AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317")))
    .WithMetrics(m => m.AddAspNetCoreInstrumentation()
                       .AddRuntimeInstrumentation()
                       .AddProcessInstrumentation()
                       .AddPrometheusExporter(o => o.ScrapeEndpointPath = "/metrics")
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
builder.Services.AddHttpClient("SettlementClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["SettlementApi__Url"] ?? "http://nginx-lb:80");
})
.AddStandardResilienceHandler();

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

app.MapPost("/api/v1/transactions", async (HttpContext context, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("SettlementClient");
    
    // Read the request body into a byte array so it can be reused for retries
    var memoryStream = new MemoryStream();
    await context.Request.Body.CopyToAsync(memoryStream);
    var bodyBytes = memoryStream.ToArray();

    // Use a strategy to wrap the SendAsync in a way that respects retries
    // Actually, SendAsync will be called by the StandardResilienceHandler.
    // If it retries, it will call our SendAsync again.
    // Wait, the ResilienceHandler is INSIDE the HttpClient.
    // When client.SendAsync(request) is called, it enters the resilience pipeline.
    // If the pipeline retries, it tries to send the SAME request object again.
    // HttpRequestMessage can only be sent once because its content is a stream.
    
    // To support retries, we might need to handle it ourselves or use a custom primary handler.
    // But a simpler way is to not use StreamContent directly if we want Polly to retry.
    
    var response = await client.PostAsync("/api/v1/transactions", new ByteArrayContent(bodyBytes)
    {
        Headers = { ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(context.Request.ContentType ?? "application/json") }
    });
    
    var content = await response.Content.ReadAsByteArrayAsync();
    
    // Create a physical response that forwards the status code and content
    context.Response.StatusCode = (int)response.StatusCode;
    foreach (var header in response.Headers)
    {
        if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }
    foreach (var header in response.Content.Headers)
    {
        if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)) continue;
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    if (content.Length > 0)
    {
        context.Response.ContentLength = content.Length;
        await context.Response.Body.WriteAsync(content);
        await context.Response.Body.FlushAsync();
    }
    return Results.Empty;
})
.WithName("ForwardTransaction")
.WithOpenApi();

app.Run();
