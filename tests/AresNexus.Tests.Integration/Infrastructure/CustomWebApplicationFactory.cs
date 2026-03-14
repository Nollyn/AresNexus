using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Marten;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using JasperFx;

namespace AresNexus.Tests.Integration.Infrastructure;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private string? _connectionString;

    public void SetConnectionString(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Provide dummy configuration to satisfy startup validation
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
                ["ConnectionStrings:PostgreSQL"] = _connectionString ?? "Host=localhost;Database=AresNexus;Username=postgres;Password=postgres"
            };
            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            // A) REMOVE ALL BACKGROUND SERVICES
            var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var service in hostedServices)
            {
                services.Remove(service);
            }

            // B) OVERRIDE HostOptions
            services.Configure<HostOptions>(options =>
            {
                options.BackgroundServiceExceptionBehavior =
                    BackgroundServiceExceptionBehavior.Ignore;
            });

            // C) REPLACE Marten configuration if NOT using real DB
            if (string.IsNullOrEmpty(_connectionString))
            {
                services.RemoveAll<IDocumentStore>();
                
                // Mock IDocumentStore with a lightweight stub to ensure no real DB connection
                var documentStoreMock = new Mock<IDocumentStore>();
                var sessionMock = new Mock<IDocumentSession>();
                var querySessionMock = new Mock<IQuerySession>();
                
                documentStoreMock.Setup(x => x.LightweightSession()).Returns(sessionMock.Object);
                documentStoreMock.Setup(x => x.QuerySession()).Returns(querySessionMock.Object);
                
                services.AddSingleton(documentStoreMock.Object);
            }
            else
            {
                // When using real DB, we must override the IDocumentStore registration 
                // to ensure Marten uses the dynamic connection string from Testcontainers.
                services.RemoveAll<IDocumentStore>();
                services.AddMarten(options =>
                {
                    options.Connection(_connectionString);
                    options.AutoCreateSchemaObjects = JasperFx.AutoCreate.All;
                }).UseLightweightSessions();
            }

            // D) Disable OpenTelemetry exporters
            // Register dummy providers to satisfy DI requirements in Program.cs while disabling actual exports
            services.RemoveAll<TracerProvider>();
            services.RemoveAll<MeterProvider>();
            services.AddSingleton(Sdk.CreateTracerProviderBuilder().Build());
            services.AddSingleton(Sdk.CreateMeterProviderBuilder().AddPrometheusExporter().Build());

            // E) Ensure Environment is forced to "Testing" is already done via builder.UseEnvironment("Testing")
            
            // STEP 5 - Add validation: If any BackgroundService starts, log a warning
            var remainingHostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            if (remainingHostedServices.Any())
            {
                Console.WriteLine("[WARNING] BackgroundService detected in test environment!");
            }
        });
    }
}
