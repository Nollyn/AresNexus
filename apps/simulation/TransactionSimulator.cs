using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AresNexus.Simulation
{
    public class TransactionSimulator
    {
        private readonly string _apiUrl;
        private readonly string _transactionsFile = "simulation/data/transactions.json";
        private readonly HttpClient _httpClient;

        public TransactionSimulator(string apiUrl)
        {
            _apiUrl = apiUrl;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task RunAsync(int maxTransactions = 1000)
        {
            if (!File.Exists(_transactionsFile))
            {
                Console.WriteLine($"Error: Transactions file not found at {_transactionsFile}");
                return;
            }

            Console.WriteLine($"Loading transactions from {_transactionsFile}...");
            var json = File.ReadAllText(_transactionsFile);
            var transactions = JsonConvert.DeserializeObject<List<Transaction>>(json) ?? new List<Transaction>();

            Console.WriteLine($"Starting simulation: Sending up to {maxTransactions} transactions to {_apiUrl}...");

            int count = 0;
            foreach (var tx in transactions)
            {
                if (count >= maxTransactions) break;

                var idempotencyKey = Guid.NewGuid();
                var payload = new
                {
                    AccountId = tx.AccountId,
                    TransactionType = tx.Type,
                    Money = new { Amount = tx.Amount, Currency = tx.Currency },
                    Reference = $"Simulation: {tx.Symbol} - {tx.Type}",
                    TraceId = $"sim-trace-{count}",
                    CorrelationId = $"sim-corr-{count}",
                    IdempotencyKey = idempotencyKey
                };

                using var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                
                // Agregamos headers requeridos
                using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl)
                {
                    Content = content
                };
                
                request.Headers.Add("Idempotency-Key", idempotencyKey.ToString());
                request.Headers.Add("X-Trace-Id", payload.TraceId);
                request.Headers.Add("X-Correlation-Id", payload.CorrelationId);

                try
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Sending request {count}...");
                    var response = await _httpClient.SendAsync(request);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Request {count} -> Status: {response.StatusCode} ({(int)response.StatusCode})");
                }
                catch (TaskCanceledException ex)
                {
                    Console.WriteLine($"[TIMEOUT] Request {count} timed out: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EXCEPTION] {ex.Message}");
                }

                count++;
                await Task.Delay(100); 
            }

            Console.WriteLine($"Simulation completed. Total transactions sent: {count}");
        }
    }
}
