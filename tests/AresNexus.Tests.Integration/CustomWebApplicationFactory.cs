using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AresNexus.Settlement.Application.Interfaces;

namespace AresNexus.Tests.Integration;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Mock out Marten and ServiceBus to avoid needing real infrastructure
            var eventStoreMock = new Mock<IEventStore>();
            services.AddScoped(_ => eventStoreMock.Object);
            
            var outboxPublisherMock = new Mock<IOutboxPublisher>();
            services.AddSingleton(_ => outboxPublisherMock.Object);

            var accountRepoMock = new Mock<IAccountRepository>();
            services.AddScoped(_ => accountRepoMock.Object);
        });
    }
}
