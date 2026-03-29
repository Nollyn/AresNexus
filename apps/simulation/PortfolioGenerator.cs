using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace AresNexus.Simulation
{
    public class Account
    {
        public Guid AccountId { get; set; }
        public string Owner { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class Position
    {
        public string PositionId { get; set; } = string.Empty;
        public Guid AccountId { get; set; }
        public string AssetClass { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal MarketValue { get; set; }
        public string Currency { get; set; } = string.Empty;
    }

    public class Transaction
    {
        public string TransactionId { get; set; } = string.Empty;
        public Guid AccountId { get; set; }
        public string PositionId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class PortfolioGenerator
    {
        private const int RandomSeed = 42;
        private readonly Random _random = new Random(RandomSeed);
        private const string OutputDir = "simulation/data";
        private const decimal TotalPortfolioValue = 5_500_000_000m;
        private const int NumAccounts = 50_000;
        private const int NumPositions = 200_000;
        private const int NumTransactions = 100_000; // Para el demo por defecto

        private readonly Dictionary<string, double> _assetDistribution = new()
        {
            { "Equities", 0.40 },
            { "Bonds", 0.30 },
            { "Derivatives", 0.10 },
            { "FX", 0.10 },
            { "Cash", 0.10 }
        };

        public void Generate()
        {
            Console.WriteLine($"Generating synthetic portfolio of ${TotalPortfolioValue / 1e9m}B USD...");

            if (!Directory.Exists(OutputDir))
                Directory.CreateDirectory(OutputDir);

            var accounts = GenerateAccounts();
            SaveToJson(accounts, "accounts.json");

            var positions = GeneratePositions(accounts);
            SaveToJson(positions, "positions.json");

            var transactions = GenerateTransactions(accounts, positions);
            SaveToJson(transactions, "transactions.json");
        }

        private List<Account> GenerateAccounts()
        {
            var accounts = new List<Account>();
            var types = new[] { "Institutional", "Retail_HighNetWorth", "Corporate" };

            for (int i = 0; i < NumAccounts; i++)
            {
                accounts.Add(new Account
                {
                    AccountId = Guid.NewGuid(),
                    Owner = $"Institutional_Client_{i}",
                    Type = types[_random.Next(types.Length)],
                    Currency = "USD",
                    CreatedAt = DateTime.Now.AddDays(-_random.Next(365, 3650))
                });
            }
            return accounts;
        }

        private List<Position> GeneratePositions(List<Account> accounts)
        {
            var positions = new List<Position>();
            decimal avgValuePerPosition = TotalPortfolioValue / NumPositions;
            var assetClasses = _assetDistribution.Keys.ToList();
            var weights = _assetDistribution.Values.ToList();

            for (int i = 0; i < NumPositions; i++)
            {
                string assetClass = GetWeightedRandom(assetClasses, weights);
                var account = accounts[_random.Next(accounts.Count)];
                decimal value = avgValuePerPosition * (decimal)(0.1 + _random.NextDouble() * 1.9);

                positions.Add(new Position
                {
                    PositionId = $"POS-{200000 + i}",
                    AccountId = account.AccountId,
                    AssetClass = assetClass,
                    Symbol = $"{(assetClass.Length >= 3 ? assetClass.Substring(0, 3).ToUpper() : assetClass.ToUpper())}-{_random.Next(100, 999)}",
                    Quantity = (decimal)(10 + _random.NextDouble() * 9990),
                    MarketValue = Math.Round(value, 2),
                    Currency = "USD"
                });
            }
            return positions;
        }

        private List<Transaction> GenerateTransactions(List<Account> accounts, List<Position> positions)
        {
            var transactions = new List<Transaction>();
            var startDate = DateTime.Now.AddDays(-30);

            for (int i = 0; i < NumTransactions; i++)
            {
                var pos = positions[_random.Next(positions.Count)];
                decimal amount = pos.MarketValue * (decimal)(0.01 + _random.NextDouble() * 0.04);

                transactions.Add(new Transaction
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    AccountId = pos.AccountId,
                    PositionId = pos.PositionId,
                    Symbol = pos.Symbol,
                    Type = "DEPOSIT", // Siempre DEPOSIT para el demo/validación inicial
                    Amount = Math.Round(amount, 2),
                    Currency = "USD",
                    Timestamp = startDate.AddSeconds(_random.Next(0, 30 * 24 * 3600)),
                    Status = "PENDING"
                });
            }
            return transactions;
        }

        private string GetWeightedRandom(List<string> items, List<double> weights)
        {
            double totalWeight = weights.Sum();
            double r = _random.NextDouble() * totalWeight;
            double sum = 0;
            for (int i = 0; i < items.Count; i++)
            {
                sum += weights[i];
                if (r <= sum) return items[i];
            }
            return items.Last();
        }

        private void SaveToJson<T>(List<T> data, string filename)
        {
            string path = Path.Combine(OutputDir, filename);
            File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
            Console.WriteLine($"Saved {data.Count} records to {path}");
        }
    }
}
