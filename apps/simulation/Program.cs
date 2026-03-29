using System;
using System.Threading.Tasks;

namespace AresNexus.Simulation
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("====================================================");
            Console.WriteLine("Ares-Nexus: Institutional Portfolio Simulation v1.0");
            Console.WriteLine("====================================================");

            string apiUrl = "http://localhost:5001/api/v1/transactions"; // Via Gateway
            
            if (args.Length > 0)
            {
                apiUrl = args[0];
            }

            int count = 100;
            if (args.Length > 1 && int.TryParse(args[1], out int parsedCount))
            {
                count = parsedCount;
            }

            var generator = new PortfolioGenerator();
            generator.Generate();

            Console.WriteLine("\nStarting transaction stream simulation...");
            var simulator = new TransactionSimulator(apiUrl);
            await simulator.RunAsync(count); // Dinámico

            Console.WriteLine("\nSimulation finished. Check Prometheus for metrics.");
        }
    }
}
