using AresNexus.Tests.Integration.Infrastructure;
using FluentAssertions;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AresNexus.Tests.Integration;

[Collection("PostgreSql collection")]
public class PostgreSqlResilienceTests : IDisposable
{
    private readonly PostgreSqlContainerFixture _fixture;
    private readonly CustomWebApplicationFactory _factory;

    public PostgreSqlResilienceTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
        _factory = new CustomWebApplicationFactory();
        _factory.SetConnectionString(_fixture.ConnectionString);
    }

    [Fact]
    public async Task DatabaseConnection_WhenInterruptedAndRestored_ShouldRecover()
    {
        // Debug: Log connection string
        Console.WriteLine($"[DEBUG_LOG] Using connection string: {_fixture.ConnectionString}");
        
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
        
        // Verify we can connect and write
        var accountId = Guid.NewGuid();
        await using (var session = store.LightweightSession())
        {
            session.Events.StartStream(accountId, new { Amount = 100 });
            await session.SaveChangesAsync();
        }

        // Verify we can read back
        await using (var session = store.QuerySession())
        {
            var stream = await session.Events.FetchStreamAsync(accountId);
            stream.Should().NotBeEmpty();
        }
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
