using InventoryKpiSystem.Core.Models;
using System;

namespace InventoryKpiSystem.ConsoleApp.Display
{
    public class ConsoleFormatter
    {
        public void DisplayKpiReport(KpiReport report)
        {
            Console.Clear();
            PrintHeader("INVENTORY KPI DASHBOARD");

            Console.WriteLine($"Generated: {report.ExportedDate:yyyy-MM-dd HH:mm:ss}\n");

            Console.WriteLine("┌─────────────────────────────────────────┬──────────────┐");
            Console.WriteLine("│ KPI                                     │ Value        │");
            Console.WriteLine("├─────────────────────────────────────────┼──────────────┤");
            Console.WriteLine($"│ Total SKUs                              │ {report.TotalProductsProcessed,12} │");
            Console.WriteLine($"│ Cost of Inventory                       │ ${report.TotalStockValue,11:N2} │");
            Console.WriteLine($"│ Out-of-Stock Items                      │ {report.SystemWide?.OutOfStockCount ?? 0,12} │");
            Console.WriteLine($"│ Average Daily Sales                     │ {report.SystemWide?.AvgDailySales ?? 0,12:F2} │");
            Console.WriteLine($"│ Average Inventory Age (days)            │ {report.SystemWide?.AvgInventoryAgeDays ?? 0.0,12:F1} │");
            Console.WriteLine("└─────────────────────────────────────────┴──────────────┘\n");
        }

        public void DisplayWelcomeBanner()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
╔═══════════════════════════════════════════════════════════╗
║                                                           ║
║      INVENTORY KPI CALCULATION SYSTEM                     ║
║      Real-Time Processing & Analytics                     ║
║                                                           ║
╚═══════════════════════════════════════════════════════════╝
            ");
            Console.ResetColor();
        }

        public void DisplayMainMenu()
        {
            Console.WriteLine("\n┌─────────────────────────────────────────┐");
            Console.WriteLine("│          MAIN MENU                      │");
            Console.WriteLine("├─────────────────────────────────────────┤");
            Console.WriteLine("│  1. View Current KPIs                   │");
            Console.WriteLine("│  2. View Product KPIs                   │");
            Console.WriteLine("│  3. View System Status                  │");
            Console.WriteLine("│  4. Generate Report                     │");
            Console.WriteLine("│  5. Configuration                       │");
            Console.WriteLine("│  Q. Quit                                │");
            Console.WriteLine("└─────────────────────────────────────────┘");
            Console.Write("\nSelect option: ");
        }

        private void PrintHeader(string title)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine($"   {title}");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");
        }

        public void DisplaySuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ {message}");
            Console.ResetColor();
        }

        public void DisplayError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ {message}");
            Console.ResetColor();
        }
    }
}