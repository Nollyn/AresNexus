using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

var client = new HttpClient();
var baseUrl = "http://localhost:5136";

Console.WriteLine("Checking /health/live...");
var live = await client.GetAsync($"{baseUrl}/health/live");
Console.WriteLine($"Result: {live.StatusCode}");

Console.WriteLine("Checking /health/ready...");
var ready = await client.GetAsync($"{baseUrl}/health/ready");
Console.WriteLine($"Result: {ready.StatusCode}");

Console.WriteLine("Checking POST /api/v1/transactions...");
var command = new { AccountId = Guid.NewGuid(), Amount = 100.0, TransactionType = "Deposit" };
var content = new StringContent(JsonSerializer.Serialize(command), Encoding.UTF8, "application/json");
var transactions = await client.PostAsync($"{baseUrl}/api/v1/transactions", content);
Console.WriteLine($"Result: {transactions.StatusCode}");
