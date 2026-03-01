using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain.Aggregates;
using Marten;

namespace AresNexus.Settlement.Api;

/// <summary>
/// Task 2: Automated Seeding and Demo Mode.
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

        // Inject 15 Demo Transactions (Task 2)
        for (int i = 1; i <= 15; i++)
        {
            account.Deposit(1000 * i, "CHF", $"Demo Deposit {i:D2}");
        }

        // Save the aggregate and its history
        await repository.SaveAsync(account, Array.Empty<object>(), stoppingToken);

        // Task 2: Inject 1 "Snapshot" upon startup
        // The repository automatically creates a snapshot if version >= 99, 
        // but the requirement explicitly says "inject 1 Snapshot upon startup".
        // Let's force one for the demo account.
        await eventStore.SaveSnapshotAsync(account.Id, account.CreateSnapshot(), account.Version);

        logger.LogInformation("Data Seeding completed. 1 Account, 15 Transactions, and 1 Snapshot created.");
    }
}
