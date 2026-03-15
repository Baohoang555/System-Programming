using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InventoryKpiSystem.ConsoleApp.Configuration;
using InventoryKpiSystem.ConsoleApp.Display;
using InventoryKpiSystem.Core.DataAccess;
using InventoryKpiSystem.Core.Models;
using InventoryKpiSystem.Core.Services;
using InventoryKpiSystem.Infrastructure.Logging;
using InventoryKpiSystem.Infrastructure.Persistence;

namespace InventoryKpiSystem.ConsoleApp.Coordinators
{
    public class ApplicationCoordinator
    {
        private readonly AppConfig _config;
        private readonly Logger _logger;
        private readonly ProcessingLogger _processingLogger;
        private readonly FileTracker _fileTracker;
        private readonly KpiRegistry _kpiRegistry;
        private readonly ConsoleFormatter _formatter;
        private readonly JsonReportGenerator _reportGenerator;

        // Core services — fully wired
        private readonly XeroDataImporter _xeroImporter;
        private readonly IncrementalKpiUpdater _kpiUpdater;
        private readonly KpiCalculator _kpiCalculator;

        private readonly Stopwatch _uptime = new();
        private readonly CancellationTokenSource _cts = new();
        private long _filesProcessed;
        private long _recordsProcessed;
        private readonly List<ProcessingError> _recentErrors = new();
        private readonly object _stateLock = new();

        private KpiReport? _currentReport;
        private Dictionary<string, ProductKpi> _currentProductKpis = new();

        public ApplicationCoordinator(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            var logLevel = config.EnableDetailedLogging ? LogLevel.Debug : LogLevel.Info;
            _logger = new Logger(config.LogDirectory, "app.log",
                config.EnableConsoleOutput, config.EnableFileOutput, logLevel);

            _processingLogger = new ProcessingLogger(_logger);
            _fileTracker = new FileTracker(config.ProcessedFilesDirectory, _logger);
            _kpiRegistry = new KpiRegistry(config.ReportsDirectory, _logger);
            _formatter = new ConsoleFormatter();
            _reportGenerator = new JsonReportGenerator(config.ReportsDirectory);

            _xeroImporter = new XeroDataImporter();
            _kpiUpdater = new IncrementalKpiUpdater();
            _kpiCalculator = new KpiCalculator();
        }

        public async Task StartAsync()
        {
            try
            {
                _formatter.DisplayWelcomeBanner();
                _processingLogger.LogSystemStartup();
                _config.ValidateAndCreateDirectories();

                _logger.LogInfo("Loading data from Xero files...");
                await LoadHistoricalDataAsync();

                _logger.LogInfo("Calculating KPIs...");
                await CalculateInitialKpisAsync();

                StartBackgroundTasks();
                _uptime.Start();

                _formatter.DisplaySuccess("System started successfully!");
                _formatter.DisplaySuccess($"Loaded {_recordsProcessed:N0} records from {_filesProcessed} files.");
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
                _logger.LogInfo("Shutdown completed");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during shutdown", ex);
            }
        }

        // ── Data Loading ─────────────────────────────────────────────────

        private async Task LoadHistoricalDataAsync()
        {
            try
            {
                // Đọc invoice files (cả ACCREC lẫn ACCPAY) từ InvoiceDirectory
                var (invoices, purchaseOrders) =
                    await _xeroImporter.ReadAllInvoiceFilesAsync(_config.InvoiceDirectory);

                // Nếu có thư mục purchase-orders riêng, đọc thêm
                if (_config.PurchaseOrderDirectory != _config.InvoiceDirectory &&
                    Directory.Exists(_config.PurchaseOrderDirectory))
                {
                    var (inv2, po2) = await _xeroImporter.ReadAllInvoiceFilesAsync(_config.PurchaseOrderDirectory);
                    invoices.AddRange(inv2);
                    purchaseOrders.AddRange(po2);
                }

                _logger.LogInfo($"Sales lines (ACCREC):    {invoices.Count}");
                _logger.LogInfo($"Purchase lines (ACCPAY): {purchaseOrders.Count}");

                _kpiUpdater.ProcessNewPurchaseOrders(purchaseOrders);
                _kpiUpdater.ProcessNewInvoices(invoices);

                lock (_stateLock)
                    _recordsProcessed = invoices.Count + purchaseOrders.Count;

                // Đếm files
                var allFiles = new List<string>();
                allFiles.AddRange(Directory.GetFiles(_config.InvoiceDirectory, "*.json"));
                allFiles.AddRange(Directory.GetFiles(_config.InvoiceDirectory, "*.txt"));
                foreach (var f in allFiles)
                {
                    if (_fileTracker.IsFileProcessed(f)) continue;
                    _fileTracker.MarkAsProcessed(f);
                    lock (_stateLock) { _filesProcessed++; }
                }

                _logger.LogInfo("Data loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load historical data", ex);
                throw;
            }
        }

        private async Task CalculateInitialKpisAsync()
        {
            var aggregates = _kpiUpdater.GetAllAggregates();

            if (aggregates.Count == 0)
            {
                _logger.LogWarning("No data found — check invoice directory path in appsettings.json");
                _currentReport = new KpiReport
                {
                    ReportId = Guid.NewGuid().ToString(),
                    ExportedDate = DateTime.Now,
                    SystemWide = new SystemWideKpi()
                };
            }
            else
            {
                _currentReport = _kpiCalculator.GenerateReport(aggregates);
                _currentProductKpis = _currentReport.Details
                    .ToDictionary(k => k.ProductId, k => k);
            }

            _formatter.DisplayKpiReport(_currentReport);
            await Task.CompletedTask;
            _logger.LogInfo($"KPIs calculated for {aggregates.Count} unique products");
        }

        // ── Main Loop ─────────────────────────────────────────────────────

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
                        case "Q": running = false; break;
                        default:
                            _formatter.DisplayError("Invalid option.");
                            continue;
                    }

                    if (running) { Console.WriteLine("\nPress any key..."); Console.ReadKey(true); }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in main loop", ex);
                    _formatter.DisplayError($"Error: {ex.Message}");
                    Console.WriteLine("\nPress any key..."); Console.ReadKey(true);
                }
            }
        }

        // ── Menu Actions ──────────────────────────────────────────────────

        private void DisplayCurrentKpis()
        {
            if (_currentReport != null) _formatter.DisplayKpiReport(_currentReport);
            else _formatter.DisplayError("No KPI data available.");
        }

        private void DisplayProductKpis()
        {
            if (_currentProductKpis.Count == 0)
            {
                _formatter.DisplayError("No product data. Add invoice files to: " + _config.InvoiceDirectory);
                return;
            }

            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine("   PRODUCT-LEVEL KPIs  (sorted by Stock Value)");
            Console.WriteLine("═══════════════════════════════════════════════════════════════════════");
            Console.WriteLine($"\n{"Product",-42} {"Stock",8} {"Value",12} {"Age(d)",8} {"OOS",5}");
            Console.WriteLine(new string('─', 80));

            int count = 0;
            foreach (var kvp in _currentProductKpis.OrderByDescending(x => x.Value.StockValue))
            {
                if (count++ >= 30) break;
                var k = kvp.Value;
                var id = k.ProductId.Length > 40 ? k.ProductId[..40] + ".." : k.ProductId;
                Console.WriteLine($"{id,-42} {k.CurrentStock,8} {k.StockValue,12:N0} {k.InventoryAgeDays,8} {(k.IsOutOfStock ? "OOS" : "-"),5}");
            }

            Console.WriteLine(new string('─', 80));
            Console.WriteLine($"Total: {_currentProductKpis.Count} products | " +
                              $"Out of Stock: {_currentProductKpis.Values.Count(v => v.IsOutOfStock)}");
        }

        private void DisplaySystemStatus()
        {
            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("   SYSTEM STATUS");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");
            Console.WriteLine($"Uptime:              {_uptime.Elapsed:hh\\:mm\\:ss}");
            Console.WriteLine($"Files Loaded:        {_filesProcessed}");
            Console.WriteLine($"Records Processed:   {_recordsProcessed:N0}");
            Console.WriteLine($"Unique Products:     {_currentProductKpis.Count}");
            Console.WriteLine($"Memory:              {GC.GetTotalMemory(false) / 1024.0 / 1024.0:F2} MB");

            Console.ForegroundColor = _recentErrors.Count > 0 ? ConsoleColor.Red : ConsoleColor.Green;
            Console.WriteLine(_recentErrors.Count > 0
                ? $"\n⚠ Recent Errors: {_recentErrors.Count}"
                : "\n✓ No recent errors");
            Console.ResetColor();
        }

        private async Task GenerateReportsAsync()
        {
            if (_currentReport == null) { _formatter.DisplayError("No data."); return; }
            Console.WriteLine("\nGenerating reports...");
            var j = await _reportGenerator.GenerateBasicReportAsync(_currentReport);
            _formatter.DisplaySuccess($"JSON:     {Path.GetFileName(j)}");
            var d = await _reportGenerator.GenerateDetailedReportAsync(_currentReport, _currentProductKpis);
            _formatter.DisplaySuccess($"Detailed: {Path.GetFileName(d)}");
            if (_currentProductKpis.Count > 0)
            {
                var c = await _reportGenerator.GenerateCsvReportAsync(_currentProductKpis);
                _formatter.DisplaySuccess($"CSV:      {Path.GetFileName(c)}");
            }
        }

        private void DisplayConfiguration()
        {
            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("   CONFIGURATION");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");
            Console.WriteLine($"Invoice Dir:       {Path.GetFullPath(_config.InvoiceDirectory)}");
            Console.WriteLine($"Reports Dir:       {Path.GetFullPath(_config.ReportsDirectory)}");
            Console.WriteLine($"Logs Dir:          {Path.GetFullPath(_config.LogDirectory)}");
            Console.WriteLine($"Auto Reports:      {_config.AutoGenerateReports}");
        }

        // ── Background Tasks ──────────────────────────────────────────────

        private void StartBackgroundTasks()
        {
            if (_config.AutoGenerateReports)
                Task.Run(AutoReportLoopAsync, _cts.Token);
            Task.Run(MemoryMonitorLoopAsync, _cts.Token);
        }

        private async Task AutoReportLoopAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(_config.ReportGenerationIntervalMinutes), _cts.Token);
                    if (_currentReport != null) await _kpiRegistry.SaveReportAsync(_currentReport);
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex) { _logger.LogError("Auto-report error", ex); }
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
                }
                catch (TaskCanceledException) { break; }
                catch (Exception ex) { _logger.LogError("Memory error", ex); }
            }
        }

        public void UpdateKpisIncremental(object newData, int recordCount)
        {
            lock (_stateLock) { _recordsProcessed += recordCount; }
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