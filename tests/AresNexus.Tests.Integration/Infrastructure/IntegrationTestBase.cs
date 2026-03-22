namespace AresNexus.Tests.Integration.Infrastructure;

public abstract class IntegrationTestBase(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    protected readonly HttpClient Client = factory.CreateClient();
    protected readonly CustomWebApplicationFactory Factory = factory;

    public virtual void Dispose()
    {
        Client.Dispose();
        // Factory is managed by xUnit as IClassFixture
    }
}
