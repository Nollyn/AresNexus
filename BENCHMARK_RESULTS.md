# AresNexus Performance Benchmarks

This file serves as a placeholder for BenchmarkDotNet results.

## Latest Run: 2026-03-03

| Method | TransactionCount | Mean | Error | StdDev | Gen0 | Allocated |
|:--- |:--- |:---:|:---:|:---:|:---:|:---:|
| **ProcessTransactions** | **1** | **TBD** | **TBD** | **TBD** | **TBD** | **TBD** |
| **ProcessTransactions** | **100** | **TBD** | **TBD** | **TBD** | **TBD** | **TBD** |
| **ProcessTransactions** | **1000** | **TBD** | **TBD** | **TBD** | **TBD** | **TBD** |

### Execution Instructions

To run benchmarks:

```bash
dotnet run -c Release --project benchmarks/AresNexus.Benchmarks/AresNexus.Benchmarks.csproj
```

*Note: Requires a local PostgreSQL instance running for connection string `Host=localhost;Database=AresNexus_Benchmarks;Username=postgres;Password=postgres`.*
