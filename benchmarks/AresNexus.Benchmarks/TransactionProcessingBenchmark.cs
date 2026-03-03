using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using AresNexus.Settlement.Domain.Aggregates;
using AresNexus.Settlement.Infrastructure.Repositories;
using AresNexus.Settlement.Domain;

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
            options.AutoCreateSchemaObjects = JasperFx.CodeGeneration.AutoCreate.All;
        });

        _serviceProvider = services.BuildServiceProvider();
        _store = _serviceProvider.GetRequiredService<IDocumentStore>();
        
        // Ensure clean state
        _store.Advanced.Clean.CompletelyRemoveAll();
    }

    [Benchmark]
    public async Task ProcessTransactions()
    {
        for (int i = 0; i < TransactionCount; i++)
        {
            var accountId = Guid.NewGuid();
            await using var session = _store.LightweightSession();
            
            var account = new Account(accountId, "CH123", "EUR");
            account.Deposit(new Money(100, "EUR"), "Initial Deposit");
            
            session.Events.StartStream<Account>(accountId, account.GetUncommittedEvents());
            await session.SaveChangesAsync();
        }
    }
}
