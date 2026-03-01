using System.Reflection;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for Structured Logging (JSON) to Console
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

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
