using System;
using System.IO;
using System.Text.Json;

namespace InventoryKpiSystem.ConsoleApp.Configuration
{
    public class AppConfig
    {
        public string InvoiceDirectory { get; set; } = "data/invoices";
        public string PurchaseOrderDirectory { get; set; } = "data/purchase-orders";
        public string ReportsDirectory { get; set; } = "data/reports";
        public string ProcessedFilesDirectory { get; set; } = "data/processed-files";
        public string LogDirectory { get; set; } = "logs";

        public int MaxConcurrentFiles { get; set; } = 5;
        public int RetryAttempts { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 2;

        public bool EnableDetailedLogging { get; set; } = true;
        public bool EnableConsoleOutput { get; set; } = true;
        public bool EnableFileOutput { get; set; } = true;

        public int ReportCleanupDays { get; set; } = 30;
        public int LogCleanupDays { get; set; } = 30;

        public bool AutoGenerateReports { get; set; } = true;
        public int ReportGenerationIntervalMinutes { get; set; } = 60;

        public static AppConfig LoadFromFile(string configPath = "appsettings.json")
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return config ?? new AppConfig();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load config: {ex.Message}");
                Console.WriteLine("Using default configuration.");
            }
            return new AppConfig();
        }

        public void SaveToFile(string configPath = "appsettings.json")
        {
            try
            {
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        public void ValidateAndCreateDirectories()
        {
            var directories = new[]
            {
                InvoiceDirectory,
                PurchaseOrderDirectory,
                ReportsDirectory,
                ProcessedFilesDirectory,
                LogDirectory
            };

            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"Created directory: {directory}");
                }
            }
        }
    }
}