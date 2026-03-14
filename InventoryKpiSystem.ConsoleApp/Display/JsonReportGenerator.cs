using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace InventoryKpiSystem.ConsoleApp.Display
{
    public class JsonReportGenerator
    {
        private readonly string _outputDirectory;

        public JsonReportGenerator(string outputDirectory = "data/reports")
        {
            _outputDirectory = outputDirectory;
            if (!Directory.Exists(_outputDirectory))
            {
                Directory.CreateDirectory(_outputDirectory);
            }
        }

        public async Task<string> GenerateBasicReportAsync(KpiReport report)
        {
            var fileName = $"kpi-report-{DateTime.Now:yyyyMMdd-HHmmss}.json";
            var filePath = Path.Combine(_outputDirectory, fileName);

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(filePath, json);
            return filePath;
        }

        public async Task<string> GenerateDetailedReportAsync(
            KpiReport systemReport,
            Dictionary<string, ProductKpi> productKpis)
        {
            var fileName = $"detailed-report-{DateTime.Now:yyyyMMdd-HHmmss}.json";
            var filePath = Path.Combine(_outputDirectory, fileName);

            var report = new
            {
                GeneratedAt = DateTime.Now,
                SystemKPIs = systemReport,
                ProductKPIs = productKpis,
                Metadata = new
                {
                    TotalProducts = productKpis.Count,
                    ReportVersion = "1.0"
                }
            };

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(filePath, json);
            return filePath;
        }

        public async Task<string> GenerateCsvReportAsync(Dictionary<string, ProductKpi> productKpis)
        {
            var fileName = $"products-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            var filePath = Path.Combine(_outputDirectory, fileName);

            using var writer = new StreamWriter(filePath);
            await writer.WriteLineAsync("ProductId,CurrentStock,StockValue,AverageAge");

            foreach (var kvp in productKpis)
            {
                await writer.WriteLineAsync(
                    $"{kvp.Key},{kvp.Value.CurrentStock},{kvp.Value.StockValue:F2},{kvp.Value.AverageAge:F2}");
            }

            return filePath;
        }
    }
}