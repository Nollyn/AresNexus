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
using AresNexus.Settlement.Application.Interfaces;
using Marten;

namespace AresNexus.Tests.Integration;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Provide dummy configuration to satisfy startup validation
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceBus:ConnectionString"] = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test",
                ["ConnectionStrings:PostgreSQL"] = "Host=localhost;Database=AresNexus;Username=postgres;Password=postgres"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // a) Remove all registered IHostedService implementations to prevent background workers from connecting
            services.RemoveAll<IHostedService>();

            // b) Replace Marten configuration with a test-safe configuration
            services.RemoveAll<IDocumentStore>();
            var documentStoreMock = new Mock<IDocumentStore>();
            var sessionMock = new Mock<IQuerySession>();
            documentStoreMock.Setup(x => x.QuerySession()).Returns(sessionMock.Object);
            documentStoreMock.Setup(x => x.LightweightSession()).Returns(new Mock<IDocumentSession>().Object);
            services.AddSingleton(documentStoreMock.Object);

            // c) Override HostOptions to ignore BackgroundService errors
            services.Configure<HostOptions>(options =>
            {
                options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            });

            // Ensure OpenTelemetry exporters are disabled or don't connect externally
            services.Replace(ServiceDescriptor.Singleton<ILoggerFactory, NullLoggerFactory>());

            // Mock out Marten and ServiceBus interfaces that might be used by the API
            var eventStoreMock = new Mock<IEventStore>();
            services.AddScoped(_ => eventStoreMock.Object);
            
            var outboxPublisherMock = new Mock<IOutboxPublisher>();
            outboxPublisherMock
                .Setup(x => x.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            services.AddSingleton(_ => outboxPublisherMock.Object);

            var accountRepoMock = new Mock<IAccountRepository>();
            services.AddScoped(_ => accountRepoMock.Object);
        });
    }
}
