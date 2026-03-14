using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using AresNexus.Services.Settlement.Domain.Aggregates;
using AresNexus.Services.Settlement.Infrastructure.Repositories;
using AresNexus.Services.Settlement.Domain;

namespace AresNexus.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<TransactionProcessingBenchmark>();
    }
}

[MemoryDiagnoser]
public class TransactionProcessingBenchmark
{
    private IDocumentStore _store = null!;
    private IServiceProvider _serviceProvider = null!;

    [Params(1, 100, 1000)]
    public int TransactionCount;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        
        // In-memory or local postgres for benchmark
        services.AddMarten(options =>
        {
            options.Connection("Host=localhost;Database=AresNexus_Benchmarks;Username=postgres;Password=postgres");
        });

        _serviceProvider = services.BuildServiceProvider();
        _store = _serviceProvider.GetRequiredService<IDocumentStore>();
        
        // Ensure clean state
    }

    [Benchmark]
    public async Task ProcessTransactions()
    {
        for (int i = 0; i < TransactionCount; i++)
        {
            var accountId = Guid.NewGuid();
            await using var session = _store.LightweightSession();
            
            var account = new Account(accountId, "CH123");
            account.Deposit(new Money(100), "Initial Deposit");
            
            session.Events.StartStream<Account>(accountId, account.GetUncommittedChanges());
            await session.SaveChangesAsync();
        }
    }
}
