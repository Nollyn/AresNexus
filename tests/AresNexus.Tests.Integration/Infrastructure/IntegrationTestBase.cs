using Xunit;

namespace AresNexus.Tests.Integration.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    protected readonly HttpClient Client;
    protected readonly CustomWebApplicationFactory Factory;

    protected IntegrationTestBase(CustomWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public virtual void Dispose()
    {
        Client.Dispose();
        // Factory is managed by xUnit as IClassFixture
    }
}
