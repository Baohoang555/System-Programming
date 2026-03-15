using InventoryKpiSystem.Core.Interfaces;
using InventoryKpiSystem.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace InventoryKpiSystem.Infrastructure.Persistence
{
    /// <summary>
    /// Manages KPI report storage and history.
    /// Uses Core.Models.KpiReport — no duplicate model definitions.
    /// </summary>
    public class KpiRegistry
    {
        private readonly string _reportsDirectory;
        private readonly ILogger? _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public KpiRegistry(string reportsDirectory = "data/reports", ILogger? logger = null)
        {
            _reportsDirectory = reportsDirectory;
            _logger = logger;

            if (!Directory.Exists(_reportsDirectory))
                Directory.CreateDirectory(_reportsDirectory);

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        // Secondary constructor accepting Infrastructure.Logging.ILogger
        public KpiRegistry(string reportsDirectory, Logging.ILogger logger)
            : this(reportsDirectory)
        {
            // logging via infrastructure logger; wrap if needed
        }

        public async Task SaveReportAsync(KpiReport report)
        {
            try
            {
                var now = DateTime.Now;
                var fileName = $"kpi-report-{now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.json";
                var filePath = Path.Combine(_reportsDirectory, fileName);

                report.ExportedDate = now;

                var json = JsonSerializer.Serialize(report, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                _logger?.LogInfo($"KPI report saved: {fileName}");
            }
            catch (Exception ex)
            {
                _logger?.LogError("Failed to save KPI report", ex);
                throw;
            }
        }

        public async Task SaveDetailedReportAsync(
            KpiReport systemReport,
            Dictionary<string, ProductKpi> productKpis)
        {
            try
            {
                var now = DateTime.Now;
                var fileName = $"detailed-report-{now:yyyyMMdd-HHmmss}.json";
                var filePath = Path.Combine(_reportsDirectory, fileName);

                var detailedReport = new
                {
                    GeneratedAt = now,
                    SystemKPIs = systemReport,
                    ProductKPIs = productKpis,
                    Metadata = new
                    {
                        TotalProducts = productKpis.Count,
                        ReportVersion = "1.0"
                    }
                };

                var json = JsonSerializer.Serialize(detailedReport, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                _logger?.LogInfo($"Detailed report saved: {fileName}");
            }
            catch (Exception ex)
            {
                _logger?.LogError("Failed to save detailed report", ex);
                throw;
            }
        }

        public async Task<KpiReport?> LoadLatestReportAsync()
        {
            try
            {
                var files = Directory.GetFiles(_reportsDirectory, "kpi-report-*.json");
                if (files.Length == 0) return null;

                var latestFile = files.OrderByDescending(f => f).First();
                var json = await File.ReadAllTextAsync(latestFile);
                return JsonSerializer.Deserialize<KpiReport>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger?.LogError("Failed to load latest report", ex);
                return null;
            }
        }

        public void CleanOldReports(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var files = Directory.GetFiles(_reportsDirectory, "*.json");
                int deletedCount = 0;

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        fileInfo.Delete();
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                    _logger?.LogInfo($"Cleaned {deletedCount} old reports");
            }
            catch (Exception ex)
            {
                _logger?.LogError("Failed to clean old reports", ex);
            }
        }
    }
}