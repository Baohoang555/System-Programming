using System;
using System.Threading.Tasks;
using InventoryKpiSystem.ConsoleApp.Configuration;
using InventoryKpiSystem.ConsoleApp.Coordinators;

namespace InventoryKpiSystem.ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ApplicationCoordinator? coordinator = null;

            try
            {
                var config = AppConfig.LoadFromFile("appsettings.json");
                coordinator = new ApplicationCoordinator(config);

                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    Console.WriteLine("\nShutting down gracefully...");
                    coordinator?.ShutdownAsync().Wait();
                };

                await coordinator.StartAsync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFATAL ERROR: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ResetColor();
                Environment.Exit(1);
            }
            finally
            {
                if (coordinator != null)
                {
                    await coordinator.ShutdownAsync();
                }
            }
        }
    }
}