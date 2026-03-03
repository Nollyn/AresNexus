using AresNexus.Shared.Kernel;

namespace AresNexus.Settlement.Api;

/// <summary>
/// Automated Seeding and Demo Mode.
/// Detects if the database is empty and automatically injects "Demo Transactions".
/// </summary>
public sealed class DataSeeder(IServiceScopeFactory scopeFactory, ILogger<DataSeeder> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var session = scope.ServiceProvider.GetRequiredService<IDocumentSession>();
        var repository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();

        // Check if we already have data
        var anyAccount = await session.Query<Account.Snapshot>().AnyAsync(stoppingToken);
        var anyStream = await session.Events.QueryAllRawEvents().AnyAsync(stoppingToken);

        if (anyAccount || anyStream)
        {
            logger.LogInformation("Database is not empty. Skipping Data Seeding.");
            return;
        }

        logger.LogInformation("Database is empty. Starting Automated Seeding of Demo Transactions...");

        // Create a Demo Account
        var accountId = Guid.Parse("d0000000-0000-0000-0000-000000000001");
        var account = new Account(accountId, "Demo Evaluator (FINMA-TIER1)");

        // Inject 5 Historical Transactions
        for (var i = 1; i <= 5; i++)
        {
            account.Deposit(new Money(1000 * i, CurrencyConstants.Chf), $"Historical Deposit {i:D2}");
        }

        // Save the aggregate and its history
        await repository.SaveAsync(account, Array.Empty<object>(), stoppingToken);

        // Inject 1 "Account Snapshot" upon startup
        await eventStore.SaveSnapshotAsync(account.Id, account.CreateSnapshot(), account.Version);

        logger.LogInformation("Data Seeding completed. 1 Account, 5 Transactions, and 1 Snapshot created.");
    }
}
