using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using InventoryKpiSystem.ConsoleApp.Configuration;
using InventoryKpiSystem.ConsoleApp.Display;
using InventoryKpiSystem.Infrastructure.Logging;
using InventoryKpiSystem.Infrastructure.Persistence;

namespace InventoryKpiSystem.ConsoleApp.Coordinators
{
    /// <summary>
    /// RESPONSIBILITIES:
    /// 1. System Startup & Shutdown
    /// 2. Component Integration
    /// 3. Background Task Management
    /// 4. User Interface Coordination
    /// 5. State Management
    /// 
    /// INTEGRATES WITH:
    /// - Thành viên 1: JsonDataReader, Models
    /// - Thành viên 2: KpiCalculator, IncrementalKpiUpdater
    /// - Thành viên 3: FileWatcher, ProcessingQueue
    /// - Thành viên 4: Logging, Persistence, Display
    /// </summary>
    public class ApplicationCoordinator
    {
        // ═══════════════════════════════════════════════════════════════════
        // FIELDS - Configuration & Infrastructure
        // ═══════════════════════════════════════════════════════════════════

        private readonly AppConfig _config;
        private readonly ILogger _logger;
        private readonly ProcessingLogger _processingLogger;
        private readonly FileTracker _fileTracker;
        private readonly KpiRegistry _kpiRegistry;
        private readonly ConsoleFormatter _formatter;
        private readonly JsonReportGenerator _reportGenerator;

        // ═══════════════════════════════════════════════════════════════════
        // FIELDS - Components từ team members khác
        // ═══════════════════════════════════════════════════════════════════
        // private readonly IJsonDataReader _dataReader;      // Thành viên 1
        // private readonly IKpiCalculator _kpiCalculator;    // Thành viên 2
        // private readonly IFileWatcher _fileWatcher;        // Thành viên 3
        // private readonly IProcessingQueue _processingQueue; // Thành viên 3

        // ═══════════════════════════════════════════════════════════════════
        // FIELDS - State Management
        // ═══════════════════════════════════════════════════════════════════

        private readonly Stopwatch _uptime;
        private readonly CancellationTokenSource _cts;
        private long _filesProcessed;
        private long _recordsProcessed;
        private readonly List<ProcessingError> _recentErrors;
        private readonly object _stateLock = new object();

        private KpiReport? _currentReport;
        private Dictionary<string, ProductKpi> _currentProductKpis;

        // ═══════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Khởi tạo ApplicationCoordinator với configuration
        /// </summary>
        public ApplicationCoordinator(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // Initialize logging infrastructure
            var logLevel = config.EnableDetailedLogging ? LogLevel.Debug : LogLevel.Info;
            _logger = new Logger(
                config.LogDirectory,
                "app.log",
                config.EnableConsoleOutput,
                config.EnableFileOutput,
                logLevel);

            _processingLogger = new ProcessingLogger(_logger);

            // Initialize persistence layer
            _fileTracker = new FileTracker(config.ProcessedFilesDirectory, _logger);
            _kpiRegistry = new KpiRegistry(config.ReportsDirectory, _logger);

            // Initialize display layer
            _formatter = new ConsoleFormatter();
            _reportGenerator = new JsonReportGenerator(config.ReportsDirectory);

            // Initialize state
            _uptime = new Stopwatch();
            _cts = new CancellationTokenSource();
            _recentErrors = new List<ProcessingError>();
            _currentProductKpis = new Dictionary<string, ProductKpi>();

            // TODO: Initialize components từ thành viên khác
            // Ví dụ:
            // _dataReader = new JsonDataReader(_logger);
            // _kpiCalculator = new KpiCalculator(_logger);
            // _fileWatcher = new FileWatcher(_logger);
            // _processingQueue = new ProcessingQueue(_config.MaxConcurrentFiles, _logger);
        }

        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC METHODS - System Lifecycle
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Khởi động toàn bộ hệ thống
        /// 
        /// WORKFLOW:
        /// 1. Display welcome banner
        /// 2. Validate configuration
        /// 3. Load historical data
        /// 4. Calculate initial KPIs
        /// 5. Start file monitoring
        /// 6. Start background tasks
        /// 7. Run main interaction loop
        /// </summary>
        public async Task StartAsync()
        {
            try
            {
                // Step 1: Display welcome
                _formatter.DisplayWelcomeBanner();
                _processingLogger.LogSystemStartup();

                // Step 2: Validate & create directories
                _logger.LogInfo("Validating configuration...");
                _config.ValidateAndCreateDirectories();

                // Step 3: Load historical data
                _logger.LogInfo("Loading historical data...");
                await LoadHistoricalDataAsync();

                // Step 4: Calculate initial KPIs
                _logger.LogInfo("Calculating initial KPIs...");
                await CalculateInitialKpisAsync();

                // Step 5: Start file monitoring
                _logger.LogInfo("Starting file monitoring...");
                StartFileMonitoring();

                // Step 6: Start background tasks
                _logger.LogInfo("Starting background services...");
                StartBackgroundTasks();

                // Step 7: Start uptime tracking
                _uptime.Start();

                _formatter.DisplaySuccess("System started successfully!");
                _logger.LogInfo($"Processed file count from previous sessions: {_fileTracker.GetProcessedFileCount()}");

                // Step 8: Run main loop (interactive menu)
                await RunMainLoopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Failed to start system", ex);
                throw;
            }
        }

        /// <summary>
        /// Shutdown hệ thống gracefully
        /// 
        /// WORKFLOW:
        /// 1. Cancel all background tasks
        /// 2. Stop file monitoring
        /// 3. Wait for processing queue to finish
        /// 4. Save final KPI report
        /// 5. Clean up resources
        /// </summary>
        public async Task ShutdownAsync()
        {
            _processingLogger.LogSystemShutdown();

            try
            {
                // Step 1: Signal cancellation
                _cts.Cancel();

                // Step 2: Stop file monitoring
                // TODO: _fileWatcher?.Stop();

                // Step 3: Wait for processing queue
                // TODO: await _processingQueue?.WaitForCompletionAsync();

                // Step 4: Save final report
                if (_currentReport != null)
                {
                    await _kpiRegistry.SaveReportAsync(_currentReport);
                    _logger.LogInfo("Final KPI report saved");
                }

                // Step 5: Log final statistics
                _processingLogger.LogStatistics(
                    totalFiles: (int)_filesProcessed,
                    successCount: (int)_filesProcessed - _recentErrors.Count,
                    failedCount: _recentErrors.Count,
                    skippedCount: 0,
                    totalDuration: _uptime.Elapsed
                );

                _logger.LogInfo("System shutdown completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during shutdown", ex);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE METHODS - Initialization
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Load dữ liệu lịch sử từ các file có sẵn trong thư mục
        /// </summary>
        private async Task LoadHistoricalDataAsync()
        {
            try
            {
                // TODO: Implement với JsonDataReader từ Thành viên 1
                /*
                var invoiceFiles = Directory.GetFiles(_config.InvoiceDirectory, "*.json");
                var purchaseOrderFiles = Directory.GetFiles(_config.PurchaseOrderDirectory, "*.json");
                
                var invoices = new List<Invoice>();
                var purchaseOrders = new List<PurchaseOrder>();
                
                foreach (var file in invoiceFiles)
                {
                    var fileInvoices = await _dataReader.ReadInvoicesAsync(file);
                    invoices.AddRange(fileInvoices);
                }
                
                foreach (var file in purchaseOrderFiles)
                {
                    var fileOrders = await _dataReader.ReadPurchaseOrdersAsync(file);
                    purchaseOrders.AddRange(fileOrders);
                }
                
                _logger.LogInfo($"Loaded {invoices.Count} invoices and {purchaseOrders.Count} purchase orders");
                */

                // Temporary placeholder
                await Task.Delay(500); // Simulate loading
                _logger.LogInfo("Historical data loaded successfully (placeholder)");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load historical data", ex);
                throw;
            }
        }

        /// <summary>
        /// Tính toán KPIs ban đầu từ historical data
        /// </summary>
        private async Task CalculateInitialKpisAsync()
        {
            try
            {
                // TODO: Implement với KpiCalculator từ Thành viên 2
                /*
                _currentReport = await _kpiCalculator.CalculateAllKpisAsync(invoices, purchaseOrders);
                _currentProductKpis = await _kpiCalculator.CalculateProductKpisAsync(invoices, purchaseOrders);
                */

                // Temporary placeholder với dummy data
                _currentReport = new KpiReport
                {
                    TotalSKUs = 0,
                    CostOfInventory = 0,
                    OutOfStockItems = 0,
                    AverageDailySales = 0,
                    AverageInventoryAge = 0,
                    GeneratedAt = DateTime.Now
                };

                // Display initial KPIs
                _formatter.DisplayKpiReport(_currentReport);

                await Task.CompletedTask;
                _logger.LogInfo("Initial KPIs calculated");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to calculate initial KPIs", ex);
                throw;
            }
        }

        /// <summary>
        /// Khởi động file monitoring system
        /// </summary>
        private void StartFileMonitoring()
        {
            try
            {
                // TODO: Implement với FileWatcher từ Thành viên 3
                /*
                _fileWatcher.MonitorDirectory(_config.InvoiceDirectory, FileType.Invoice);
                _fileWatcher.MonitorDirectory(_config.PurchaseOrderDirectory, FileType.PurchaseOrder);
                
                _fileWatcher.OnFileDetected += async (sender, args) =>
                {
                    await HandleNewFileAsync(args.FilePath, args.FileType);
                };
                
                _fileWatcher.Start();
                */

                _logger.LogInfo($"File monitoring started:");
                _logger.LogInfo($"  - Invoices: {_config.InvoiceDirectory}");
                _logger.LogInfo($"  - Purchase Orders: {_config.PurchaseOrderDirectory}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to start file monitoring", ex);
                throw;
            }
        }

        /// <summary>
        /// Khởi động các background tasks
        /// </summary>
        private void StartBackgroundTasks()
        {
            // Task 1: Auto-generate reports periodically
            if (_config.AutoGenerateReports)
            {
                Task.Run(async () => await AutoReportGenerationLoopAsync(), _cts.Token);
                _logger.LogInfo($"Auto-report generation enabled (every {_config.ReportGenerationIntervalMinutes} minutes)");
            }

            // Task 2: Periodic cleanup of old files
            Task.Run(async () => await PeriodicCleanupLoopAsync(), _cts.Token);
            _logger.LogInfo("Periodic cleanup task started");

            // Task 3: Memory monitoring
            Task.Run(async () => await MemoryMonitorLoopAsync(), _cts.Token);
            _logger.LogInfo("Memory monitoring started");

            // Task 4: Statistics logging
            Task.Run(async () => await StatisticsLoggingLoopAsync(), _cts.Token);
            _logger.LogInfo("Statistics logging started");
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE METHODS - Main Loop (User Interaction)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Main interaction loop - xử lý user input
        /// </summary>
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
                        case "1":
                            DisplayCurrentKpis();
                            break;

                        case "2":
                            DisplayProductKpis();
                            break;

                        case "3":
                            DisplaySystemStatus();
                            break;

                        case "4":
                            await GenerateReportsAsync();
                            break;

                        case "5":
                            DisplayConfiguration();
                            break;

                        case "Q":
                            _logger.LogInfo("User requested shutdown");
                            running = false;
                            break;

                        default:
                            _formatter.DisplayError("Invalid option. Please try again.");
                            continue; // Don't wait for keypress on invalid input
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

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE METHODS - Menu Actions
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Display current system-wide KPIs
        /// </summary>
        private void DisplayCurrentKpis()
        {
            if (_currentReport != null)
            {
                _formatter.DisplayKpiReport(_currentReport);
            }
            else
            {
                _formatter.DisplayError("No KPI data available yet.");
                _logger.LogWarning("Attempted to display KPIs but no data available");
            }
        }

        /// <summary>
        /// Display product-level KPIs
        /// </summary>
        private void DisplayProductKpis()
        {
            if (_currentProductKpis.Count > 0)
            {
                Console.Clear();
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("   PRODUCT-LEVEL KPIs");
                Console.WriteLine("═══════════════════════════════════════════════════════════\n");

                Console.WriteLine("┌──────────────┬────────────┬──────────────┬─────────────┐");
                Console.WriteLine("│ Product ID   │ Stock      │ Stock Value  │ Age (days)  │");
                Console.WriteLine("├──────────────┼────────────┼──────────────┼─────────────┤");

                var count = 0;
                foreach (var kvp in _currentProductKpis)
                {
                    if (count >= 20) break; // Limit to 20 for display

                    var productId = kvp.Key.Length > 12 ? kvp.Key.Substring(0, 12) : kvp.Key;
                    var kpi = kvp.Value;

                    Console.WriteLine($"│ {productId,-12} │ {kpi.CurrentStock,10} │ ${kpi.StockValue,11:N2} │ {kpi.AverageAge,11:F1} │");
                    count++;
                }

                Console.WriteLine("└──────────────┴────────────┴──────────────┴─────────────┘");

                if (_currentProductKpis.Count > 20)
                {
                    Console.WriteLine($"\n(Showing top 20 of {_currentProductKpis.Count} products)");
                }
            }
            else
            {
                _formatter.DisplayError("No product data available yet.");
                _logger.LogWarning("Attempted to display product KPIs but no data available");
            }
        }

        /// <summary>
        /// Display system status and statistics
        /// </summary>
        private void DisplaySystemStatus()
        {
            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("   SYSTEM STATUS");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            Console.WriteLine($"Uptime:                {_uptime.Elapsed:hh\\:mm\\:ss}");
            Console.WriteLine($"Files Processed:       {_filesProcessed}");
            Console.WriteLine($"Records Processed:     {_recordsProcessed:N0}");

            var memoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
            Console.WriteLine($"Memory Usage:          {memoryMB:F2} MB");

            var filesPerHour = _uptime.Elapsed.TotalHours > 0
                ? _filesProcessed / _uptime.Elapsed.TotalHours
                : 0;
            Console.WriteLine($"Files/Hour:            {filesPerHour:F2}");

            Console.WriteLine($"Last Update:           {DateTime.Now:HH:mm:ss}");
            Console.WriteLine($"Total Files Tracked:   {_fileTracker.GetProcessedFileCount()}");

            Console.WriteLine("\n" + new string('─', 59));

            if (_recentErrors.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n⚠ Recent Errors: {_recentErrors.Count}");
                Console.ResetColor();

                foreach (var error in _recentErrors.Take(5))
                {
                    Console.WriteLine($"  [{error.Timestamp:HH:mm:ss}] {error.FileName}: {error.ErrorMessage}");
                }

                if (_recentErrors.Count > 5)
                {
                    Console.WriteLine($"  ... and {_recentErrors.Count - 5} more");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n✓ No recent errors");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Generate and save KPI reports
        /// </summary>
        private async Task GenerateReportsAsync()
        {
            try
            {
                Console.WriteLine("\nGenerating reports...");

                if (_currentReport == null)
                {
                    _formatter.DisplayError("No data available to generate reports.");
                    return;
                }

                // Generate basic JSON report
                var jsonPath = await _reportGenerator.GenerateBasicReportAsync(_currentReport);
                _formatter.DisplaySuccess($"JSON report: {Path.GetFileName(jsonPath)}");

                // Generate detailed report
                var detailedPath = await _reportGenerator.GenerateDetailedReportAsync(
                    _currentReport,
                    _currentProductKpis);
                _formatter.DisplaySuccess($"Detailed report: {Path.GetFileName(detailedPath)}");

                // Generate CSV if we have product data
                if (_currentProductKpis.Count > 0)
                {
                    var csvPath = await _reportGenerator.GenerateCsvReportAsync(_currentProductKpis);
                    _formatter.DisplaySuccess($"CSV report: {Path.GetFileName(csvPath)}");
                }

                _logger.LogInfo("Reports generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to generate reports", ex);
                _formatter.DisplayError($"Failed to generate reports: {ex.Message}");
            }
        }

        /// <summary>
        /// Display current configuration
        /// </summary>
        private void DisplayConfiguration()
        {
            Console.Clear();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("   SYSTEM CONFIGURATION");
            Console.WriteLine("═══════════════════════════════════════════════════════════\n");

            Console.WriteLine($"Invoice Directory:         {_config.InvoiceDirectory}");
            Console.WriteLine($"Purchase Order Directory:  {_config.PurchaseOrderDirectory}");
            Console.WriteLine($"Reports Directory:         {_config.ReportsDirectory}");
            Console.WriteLine($"Logs Directory:            {_config.LogDirectory}");
            Console.WriteLine();
            Console.WriteLine($"Max Concurrent Files:      {_config.MaxConcurrentFiles}");
            Console.WriteLine($"Retry Attempts:            {_config.RetryAttempts}");
            Console.WriteLine($"Retry Delay:               {_config.RetryDelaySeconds}s");
            Console.WriteLine();
            Console.WriteLine($"Detailed Logging:          {_config.EnableDetailedLogging}");
            Console.WriteLine($"Console Output:            {_config.EnableConsoleOutput}");
            Console.WriteLine($"File Output:               {_config.EnableFileOutput}");
            Console.WriteLine();
            Console.WriteLine($"Auto Generate Reports:     {_config.AutoGenerateReports}");
            Console.WriteLine($"Report Interval:           {_config.ReportGenerationIntervalMinutes} minutes");
            Console.WriteLine($"Report Cleanup Days:       {_config.ReportCleanupDays}");
            Console.WriteLine($"Log Cleanup Days:          {_config.LogCleanupDays}");
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE METHODS - Background Tasks
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Background task: Auto-generate reports periodically
        /// </summary>
        private async Task AutoReportGenerationLoopAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(
                        TimeSpan.FromMinutes(_config.ReportGenerationIntervalMinutes),
                        _cts.Token);

                    if (_currentReport != null)
                    {
                        await _kpiRegistry.SaveReportAsync(_currentReport);
                        _logger.LogInfo("Auto-generated report saved");
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in auto-report generation", ex);
                }
            }
        }

        /// <summary>
        /// Background task: Periodic cleanup of old files
        /// </summary>
        private async Task PeriodicCleanupLoopAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // Run cleanup once per day at 2 AM
                    var now = DateTime.Now;
                    var nextRun = now.Date.AddDays(1).AddHours(2);
                    var delay = nextRun - now;

                    await Task.Delay(delay, _cts.Token);

                    _logger.LogInfo("Running periodic cleanup...");
                    _kpiRegistry.CleanOldReports(_config.ReportCleanupDays);
                    _logger.LogInfo("Periodic cleanup completed");
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in periodic cleanup", ex);
                }
            }
        }

        /// <summary>
        /// Background task: Monitor memory usage
        /// </summary>
        private async Task MemoryMonitorLoopAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), _cts.Token);

                    _processingLogger.LogMemoryUsage();

                    // Force GC if memory is high
                    var memoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
                    if (memoryMB > 500) // 500 MB threshold
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();

                        var newMemoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
                        _logger.LogWarning($"Forced GC: {memoryMB:F2} MB → {newMemoryMB:F2} MB");
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in memory monitor", ex);
                }
            }
        }

        /// <summary>
        /// Background task: Log statistics periodically
        /// </summary>
        private async Task StatisticsLoggingLoopAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(15), _cts.Token);

                    _logger.LogInfo($"Statistics: {_filesProcessed} files processed, {_recordsProcessed:N0} records, uptime: {_uptime.Elapsed:hh\\:mm\\:ss}");
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in statistics logging", ex);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE METHODS - File Processing (Integration với Thành viên 3)
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Handle khi FileWatcher detect file mới
        /// </summary>
        private async Task HandleNewFileAsync(string filePath, string fileType)
        {
            try
            {
                _processingLogger.LogFileDetected(filePath, fileType);

                // Check if already processed
                if (_fileTracker.IsFileProcessed(filePath))
                {
                    _processingLogger.LogFileSkipped(filePath, "Already processed");
                    return;
                }

                // TODO: Add to processing queue (Thành viên 3's component)
                // _processingQueue.Enqueue(new FileTask
                // {
                //     FilePath = filePath,
                //     FileType = fileType
                // });

                // Placeholder: Simulate processing
                await Task.Delay(100);

                // Mark as processed
                _fileTracker.MarkAsProcessed(filePath);

                // Update statistics
                lock (_stateLock)
                {
                    _filesProcessed++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error handling new file: {filePath}", ex);

                // Track error
                lock (_stateLock)
                {
                    _recentErrors.Add(new ProcessingError
                    {
                        Timestamp = DateTime.Now,
                        FileName = Path.GetFileName(filePath),
                        ErrorMessage = ex.Message
                    });

                    // Keep only last 50 errors
                    if (_recentErrors.Count > 50)
                    {
                        _recentErrors.RemoveAt(0);
                    }
                }
            }
        }

        /// <summary>
        /// Update KPIs with new data (incremental update)
        /// Called after processing new file
        /// </summary>
        public void UpdateKpisIncremental(object newData, int recordCount)
        {
            try
            {
                // TODO: Implement với IncrementalKpiUpdater từ Thành viên 2
                // _kpiUpdater.UpdateWithNewData(newData);
                // _currentReport = _kpiUpdater.GetCurrentReport();
                // _currentProductKpis = _kpiUpdater.GetProductKpis();

                lock (_stateLock)
                {
                    _recordsProcessed += recordCount;
                }

                _logger.LogDebug($"KPIs updated with {recordCount} new records");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error updating KPIs", ex);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // SUPPORTING CLASSES
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Represents a processing error for tracking
    /// </summary>
    public class ProcessingError
    {
        public DateTime Timestamp { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
    }
}

/*
 * ═══════════════════════════════════════════════════════════════════════════════
 * INTEGRATION NOTES FOR TEAM MEMBERS
 * ═══════════════════════════════════════════════════════════════════════════════
 * 
 * THÀNH VIÊN 1 (Data Layer):
 * - Provide: IJsonDataReader interface
 * - Implement: ReadInvoicesAsync(), ReadPurchaseOrdersAsync()
 * - Models: Invoice, PurchaseOrder, Product
 * 
 * THÀNH VIÊN 2 (KPI Calculator):
 * - Provide: IKpiCalculator interface
 * - Implement: CalculateAllKpisAsync(), CalculateProductKpisAsync()
 * - Provide: IncrementalKpiUpdater class
 * 
 * THÀNH VIÊN 3 (File Processing):
 * - Provide: IFileWatcher interface with OnFileDetected event
 * - Provide: IProcessingQueue interface
 * - Implement: Background processing với retry logic
 * 
 * ═══════════════════════════════════════════════════════════════════════════════
 */