using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryKpiSystem.ConsoleApp.Configuration;
using InventoryKpiSystem.ConsoleApp.Display;
using InventoryKpiSystem.Core.Models;
using InventoryKpiSystem.Infrastructure.Logging;
using InventoryKpiSystem.Infrastructure.Persistence;

namespace InventoryKpiSystem.ConsoleApp.Coordinators
{
    public class ApplicationCoordinator
    {
        // ── Infrastructure ───────────────────────────────────────────────
        private readonly AppConfig _config;
        private readonly Logger _logger;
        private readonly ProcessingLogger _processingLogger;
        private readonly FileTracker _fileTracker;
        private readonly KpiRegistry _kpiRegistry;
        private readonly ConsoleFormatter _formatter;
        private readonly JsonReportGenerator _reportGenerator;

        // ── State ────────────────────────────────────────────────────────
        private readonly Stopwatch _uptime = new();
        private readonly CancellationTokenSource _cts = new();
        private long _filesProcessed;
        private long _recordsProcessed;
        private readonly List<ProcessingError> _recentErrors = new();
        private readonly object _stateLock = new();

        // ── KPI state (Core.Models — single source of truth) ─────────────
        private KpiReport? _currentReport;
        private Dictionary<string, ProductKpi> _currentProductKpis = new();

        // ════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ════════════════════════════════════════════════════════════════

        public ApplicationCoordinator(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            var logLevel = config.EnableDetailedLogging ? LogLevel.Debug : LogLevel.Info;
            _logger = new Logger(
                config.LogDirectory, "app.log",
                config.EnableConsoleOutput, config.EnableFileOutput, logLevel);

            _processingLogger = new ProcessingLogger(_logger);
            _fileTracker = new FileTracker(config.ProcessedFilesDirectory, _logger);
            _kpiRegistry = new KpiRegistry(config.ReportsDirectory, _logger);
            _formatter = new ConsoleFormatter();
            _reportGenerator = new JsonReportGenerator(config.ReportsDirectory);
        }

        // ════════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ════════════════════════════════════════════════════════════════

        public async Task StartAsync()
        {
            try
            {
                _formatter.DisplayWelcomeBanner();
                _processingLogger.LogSystemStartup();

                _config.ValidateAndCreateDirectories();
                await LoadHistoricalDataAsync();
                await CalculateInitialKpisAsync();
                StartFileMonitoring();
                StartBackgroundTasks();

                _uptime.Start();
                _formatter.DisplaySuccess("System started successfully!");
                _logger.LogInfo($"Previously processed files: {_fileTracker.GetProcessedFileCount()}");

                await RunMainLoopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Failed to start system", ex);
                throw;
            }
        }

        public async Task ShutdownAsync()
        {
            _processingLogger.LogSystemShutdown();
            try
            {
                _cts.Cancel();

                if (_currentReport != null)
                    await _kpiRegistry.SaveReportAsync(_currentReport);

                _processingLogger.LogStatistics(
                    (int)_filesProcessed,
                    (int)_filesProcessed - _recentErrors.Count,
                    _recentErrors.Count, 0, _uptime.Elapsed);

                _logger.LogInfo("Shutdown completed");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during shutdown", ex);
            }
        }

        // ════════════════════════════════════════════════════════════════
        // INITIALIZATION
        // ════════════════════════════════════════════════════════════════

        private async Task LoadHistoricalDataAsync()
        {
            // TODO: wire up JsonDataReader from Core once data files are in place
            await Task.Delay(200);
            _logger.LogInfo("Historical data loaded (placeholder)");
        }

        private async Task CalculateInitialKpisAsync()
        {
            // Placeholder — replaced by real KpiCalculator output once data flows in
            _currentReport = new KpiReport
            {
                ReportId = Guid.NewGuid().ToString(),
                ExportedDate = DateTime.Now,
                SystemWide = new SystemWideKpi()
            };

            _formatter.DisplayKpiReport(_currentReport);
            await Task.CompletedTask;
            _logger.LogInfo("Initial KPIs calculated");
        }

        private void StartFileMonitoring()
        {
            // TODO: wire up InventoryWatcher
            _logger.LogInfo($"Monitoring: {_config.InvoiceDirectory}, {_config.PurchaseOrderDirectory}");
        }

        private void StartBackgroundTasks()
        {
            if (_config.AutoGenerateReports)
                Task.Run(AutoReportGenerationLoopAsync, _cts.Token);

            Task.Run(PeriodicCleanupLoopAsync, _cts.Token);
            Task.Run(MemoryMonitorLoopAsync, _cts.Token);
            Task.Run(StatisticsLoggingLoopAsync, _cts.Token);
        }

        // ════════════════════════════════════════════════════════════════
        // MAIN LOOP
        // ════════════════════════════════════════════════════════════════

        private async Task RunMainLoopAsync()
        {
            bool running = true;
            while (running && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    _formatter.DisplayMainMenu();
                    var input = Console.ReadLine()?.Trim().ToUpper();

                    switch (input)
                    {
                        case "1": DisplayCurrentKpis(); break;
                        case "2": DisplayProductKpis(); break;
                        case "3": DisplaySystemStatus(); break;
                        case "4": await GenerateReportsAsync(); break;
                        case "5": DisplayConfiguration(); break;
                        case "Q":
                            _logger.LogInfo("User requested shutdown");
                            running = false;
                            break;
                        default:
                            _formatter.DisplayError("Invalid option.");
                            continue;
                    }

                    if (running)
                    {
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey(true);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in main loop", ex);
                    _formatter.DisplayError($"Error: {ex.Message}");
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey(true);
                }
            }
        }

        // ════════════════════════════════════════════════════════════════
        // MENU ACTIONS
        // ════════════════════════════════════════════════════════════════

        private void DisplayCurrentKpis()
        {
            if (_currentReport != null)
                _formatter.DisplayKpiReport(_currentReport);
            else
                _formatter.DisplayError("No KPI data available yet.");
        }

        private void DisplayProductKpis()
        {
            if (_currentProductKpis.Count == 0)
            {
                _formatter.DisplayError("No product data available yet.");
                return;
            }

            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("   PRODUCT-LEVEL KPIs");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");
            Console.WriteLine("┌──────────────┬────────────┬──────────────┬─────────────┐");
            Console.WriteLine("│ Product ID   │ Stock      │ Stock Value  │ Age (days)  │");
            Console.WriteLine("├──────────────┼────────────┼──────────────┼─────────────┤");

            int count = 0;
            foreach (var kvp in _currentProductKpis)
            {
                if (count++ >= 20) break;
                var id = kvp.Key.Length > 12 ? kvp.Key[..12] : kvp.Key;
                var k = kvp.Value;
                Console.WriteLine($"│ {id,-12} │ {k.CurrentStock,10} │ ${k.StockValue,11:N2} │ {k.InventoryAgeDays,11} │");
            }

            Console.WriteLine("└──────────────┴────────────┴──────────────┴─────────────┘");
            if (_currentProductKpis.Count > 20)
                Console.WriteLine($"\n(Showing top 20 of {_currentProductKpis.Count})");
        }

        private void DisplaySystemStatus()
        {
            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("   SYSTEM STATUS");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");
            Console.WriteLine($"Uptime:              {_uptime.Elapsed:hh\\:mm\\:ss}");
            Console.WriteLine($"Files Processed:     {_filesProcessed}");
            Console.WriteLine($"Records Processed:   {_recordsProcessed:N0}");
            Console.WriteLine($"Memory:              {GC.GetTotalMemory(false) / 1024.0 / 1024.0:F2} MB");
            Console.WriteLine($"Tracked Files:       {_fileTracker.GetProcessedFileCount()}");

            if (_recentErrors.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n⚠ Recent Errors: {_recentErrors.Count}");
                Console.ResetColor();
                foreach (var e in _recentErrors.Take(5))
                    Console.WriteLine($"  [{e.Timestamp:HH:mm:ss}] {e.FileName}: {e.ErrorMessage}");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ No recent errors");
                Console.ResetColor();
            }
        }

        private async Task GenerateReportsAsync()
        {
            if (_currentReport == null)
            {
                _formatter.DisplayError("No data available to generate reports.");
                return;
            }

            Console.WriteLine("\nGenerating reports...");

            var jsonPath = await _reportGenerator.GenerateBasicReportAsync(_currentReport);
            _formatter.DisplaySuccess($"JSON: {Path.GetFileName(jsonPath)}");

            var detailPath = await _reportGenerator.GenerateDetailedReportAsync(_currentReport, _currentProductKpis);
            _formatter.DisplaySuccess($"Detailed: {Path.GetFileName(detailPath)}");

            if (_currentProductKpis.Count > 0)
            {
                var csvPath = await _reportGenerator.GenerateCsvReportAsync(_currentProductKpis);
                _formatter.DisplaySuccess($"CSV: {Path.GetFileName(csvPath)}");
            }
        }

        private void DisplayConfiguration()
        {
            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("   CONFIGURATION");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");
            Console.WriteLine($"Invoice Dir:       {_config.InvoiceDirectory}");
            Console.WriteLine($"Purchase Order Dir:{_config.PurchaseOrderDirectory}");
            Console.WriteLine($"Reports Dir:       {_config.ReportsDirectory}");
            Console.WriteLine($"Logs Dir:          {_config.LogDirectory}");
            Console.WriteLine($"Max Concurrent:    {_config.MaxConcurrentFiles}");
            Console.WriteLine($"Retry Attempts:    {_config.RetryAttempts}");
            Console.WriteLine($"Auto Reports:      {_config.AutoGenerateReports}");
            Console.WriteLine($"Report Interval:   {_config.ReportGenerationIntervalMinutes} min");
        }

        // ════════════════════════════════════════════════════════════════
        // BACKGROUND TASKS
        // ════════════════════════════════════════════════════════════════

        private async Task AutoReportGenerationLoopAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(
                        TimeSpan.FromMinutes(_config.ReportGenerationIntervalMinutes), _cts.Token);
                    if (_currentReport != null)
                        await _kpiRegistry.SaveReportAsync(_currentReport);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex) { _logger.LogError("Auto-report error", ex); }
            }
        }

        private async Task PeriodicCleanupLoopAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var delay = DateTime.Now.Date.AddDays(1).AddHours(2) - DateTime.Now;
                    await Task.Delay(delay, _cts.Token);
                    _kpiRegistry.CleanOldReports(_config.ReportCleanupDays);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex) { _logger.LogError("Cleanup error", ex); }
            }
        }

        private async Task MemoryMonitorLoopAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), _cts.Token);
                    _processingLogger.LogMemoryUsage();
                    var mb = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
                    if (mb > 500)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        _logger.LogWarning($"Forced GC: was {mb:F1} MB");
                    }
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex) { _logger.LogError("Memory monitor error", ex); }
            }
        }

        private async Task StatisticsLoggingLoopAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(15), _cts.Token);
                    _logger.LogInfo($"Stats: {_filesProcessed} files, {_recordsProcessed:N0} records, uptime {_uptime.Elapsed:hh\\:mm\\:ss}");
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex) { _logger.LogError("Stats logging error", ex); }
            }
        }

        // ════════════════════════════════════════════════════════════════
        // FILE PROCESSING
        // ════════════════════════════════════════════════════════════════

        private async Task HandleNewFileAsync(string filePath, string fileType)
        {
            try
            {
                _processingLogger.LogFileDetected(filePath, fileType);

                if (_fileTracker.IsFileProcessed(filePath))
                {
                    _processingLogger.LogFileSkipped(filePath, "Already processed");
                    return;
                }

                await Task.Delay(100); // placeholder for real processing
                _fileTracker.MarkAsProcessed(filePath);

                lock (_stateLock) { _filesProcessed++; }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling file: {filePath}", ex);
                lock (_stateLock)
                {
                    _recentErrors.Add(new ProcessingError
                    {
                        Timestamp = DateTime.Now,
                        FileName = Path.GetFileName(filePath),
                        ErrorMessage = ex.Message
                    });
                    if (_recentErrors.Count > 50)
                        _recentErrors.RemoveAt(0);
                }
            }
        }

        public void UpdateKpisIncremental(object newData, int recordCount)
        {
            lock (_stateLock) { _recordsProcessed += recordCount; }
            _logger.LogDebug($"KPIs updated with {recordCount} records");
        }
    }

    public class ProcessingError
    {
        public DateTime Timestamp { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
    }
}