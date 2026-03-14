
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace InventoryKpiSystem.Infrastructure.Logging
{

    public class ProcessingLogger
    {
        // ═══════════════════════════════════════════════════════════════════
        // FIELDS
        // ═══════════════════════════════════════════════════════════════════

        private readonly ILogger _logger;
        private readonly Dictionary<string, Stopwatch> _timers;
        private readonly object _lockObject = new object();

        // ═══════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════
        /// <param name="logger">Base logger to use for output</param>
        public ProcessingLogger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _timers = new Dictionary<string, Stopwatch>();
        }

        // ═══════════════════════════════════════════════════════════════════
        // FILE PROCESSING EVENTS
        // ═══════════════════════════════════════════════════════════════════
        /// <param name="filePath">Full path to detected file</param>
        /// <param name="fileType">Type of file (Invoice, PurchaseOrder, etc.)</param>
        public void LogFileDetected(string filePath, string fileType)
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            _logger.LogInfo($"📁 New {fileType} detected: {fileName}");
        }
        /// Log khi bắt đầu xử lý file và start timer
        /// </summary>
        /// <param name="filePath">Full path to file being processed</param>
        /// <param name="fileType">Type of file</param>
        public void LogProcessingStart(string filePath, string fileType)
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            _logger.LogInfo($"⚙️  Processing started: {fileName} ({fileType})");

            // Start timer for this file
            lock (_lockObject)
            {
                if (!_timers.ContainsKey(filePath))
                {
                    _timers[filePath] = Stopwatch.StartNew();
                }
            }
        }

        /// <summary>
        /// Log khi xử lý file thành công và stop timer
        /// </summary>
        /// <param name="filePath">Full path to processed file</param>
        /// <param name="duration">Processing duration</param>
        /// <param name="recordsProcessed">Number of records processed</param>
        public void LogProcessingComplete(string filePath, TimeSpan duration, int recordsProcessed)
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            _logger.LogInfo($"✅ Completed: {fileName} | {duration.TotalSeconds:F2}s | {recordsProcessed} records");

            // Stop and remove timer
            lock (_lockObject)
            {
                if (_timers.ContainsKey(filePath))
                {
                    _timers[filePath].Stop();
                    _timers.Remove(filePath);
                }
            }
        }

        /// <summary>
        /// Log khi xử lý file thành công (tự động tính duration từ timer)
        /// </summary>
        /// <param name="filePath">Full path to processed file</param>
        /// <param name="recordsProcessed">Number of records processed</param>
        public void LogProcessingCompleteAuto(string filePath, int recordsProcessed)
        {
            TimeSpan duration = TimeSpan.Zero;

            lock (_lockObject)
            {
                if (_timers.TryGetValue(filePath, out var timer))
                {
                    timer.Stop();
                    duration = timer.Elapsed;
                    _timers.Remove(filePath);
                }
            }

            LogProcessingComplete(filePath, duration, recordsProcessed);
        }

        /// <summary>
        /// Log khi xử lý file thất bại
        /// </summary>
        /// <param name="filePath">Full path to failed file</param>
        /// <param name="exception">Exception that occurred</param>
        public void LogProcessingFailed(string filePath, Exception exception)
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            _logger.LogError($"❌ Failed: {fileName}", exception);

            // Stop and remove timer
            lock (_lockObject)
            {
                if (_timers.ContainsKey(filePath))
                {
                    _timers[filePath].Stop();
                    _timers.Remove(filePath);
                }
            }
        }

        /// <summary>
        /// Log khi file bị skip (duplicate, already processed, etc.)
        /// </summary>
        /// <param name="filePath">Full path to skipped file</param>
        /// <param name="reason">Reason for skipping</param>
        public void LogFileSkipped(string filePath, string reason)
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            _logger.LogWarning($"⏭️  Skipped: {fileName} | Reason: {reason}");
        }

        /// <summary>
        /// Log khi retry xử lý file
        /// </summary>
        /// <param name="filePath">Full path to file being retried</param>
        /// <param name="attemptNumber">Current retry attempt (1-based)</param>
        /// <param name="maxAttempts">Maximum retry attempts</param>
        public void LogRetryAttempt(string filePath, int attemptNumber, int maxAttempts)
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            _logger.LogWarning($"🔄 Retry {attemptNumber}/{maxAttempts}: {fileName}");
        }

        /// <summary>
        /// Log khi file validation thất bại
        /// </summary>
        /// <param name="filePath">Full path to file</param>
        /// <param name="errorMessage">Validation error message</param>
        public void LogValidationError(string filePath, string errorMessage)
        {
            var fileName = System.IO.Path.GetFileName(filePath);
            _logger.LogError($"⚠️  Validation error in {fileName}: {errorMessage}");
        }

        // ═══════════════════════════════════════════════════════════════════
        // BATCH PROCESSING
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Log start of batch processing
        /// </summary>
        /// <param name="batchId">Unique identifier for batch</param>
        /// <param name="fileCount">Number of files in batch</param>
        public void LogBatchStart(string batchId, int fileCount)
        {
            _logger.LogInfo($"📦 Batch {batchId} started: {fileCount} files");
        }

        /// <summary>
        /// Log completion of batch processing
        /// </summary>
        /// <param name="batchId">Unique identifier for batch</param>
        /// <param name="totalFiles">Total files in batch</param>
        /// <param name="successCount">Number of successful files</param>
        /// <param name="failedCount">Number of failed files</param>
        /// <param name="duration">Total batch processing time</param>
        public void LogBatchComplete(string batchId, int totalFiles, int successCount, int failedCount, TimeSpan duration)
        {
            _logger.LogInfo($"📦 Batch {batchId} completed: {successCount}/{totalFiles} succeeded, {failedCount} failed | {duration.TotalSeconds:F2}s");
        }

        // ═══════════════════════════════════════════════════════════════════
        // STATISTICS & SUMMARIES
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Log statistics tổng quan với beautiful formatting
        /// </summary>
        /// <param name="totalFiles">Total files processed</param>
        /// <param name="successCount">Number of successful files</param>
        /// <param name="failedCount">Number of failed files</param>
        /// <param name="skippedCount">Number of skipped files</param>
        /// <param name="totalDuration">Total processing time</param>
        public void LogStatistics(int totalFiles, int successCount, int failedCount, int skippedCount, TimeSpan totalDuration)
        {
            _logger.LogInfo("═══════════════════════════════════════════");
            _logger.LogInfo("         PROCESSING STATISTICS             ");
            _logger.LogInfo("═══════════════════════════════════════════");
            _logger.LogInfo($"Total Files:       {totalFiles}");
            _logger.LogInfo($"Successful:        {successCount} ({CalculatePercentage(successCount, totalFiles):F1}%)");
            _logger.LogInfo($"Failed:            {failedCount} ({CalculatePercentage(failedCount, totalFiles):F1}%)");
            _logger.LogInfo($"Skipped:           {skippedCount} ({CalculatePercentage(skippedCount, totalFiles):F1}%)");
            _logger.LogInfo($"Total Duration:    {totalDuration.TotalSeconds:F2}s");
            _logger.LogInfo($"Avg per file:      {(totalFiles > 0 ? totalDuration.TotalSeconds / totalFiles : 0):F2}s");
            _logger.LogInfo($"Throughput:        {(totalDuration.TotalSeconds > 0 ? totalFiles / totalDuration.TotalSeconds * 60 : 0):F1} files/min");
            _logger.LogInfo("═══════════════════════════════════════════");
        }

        /// <summary>
        /// Log detailed statistics with file type breakdown
        /// </summary>
        public void LogDetailedStatistics(Dictionary<string, FileTypeStats> statsByType)
        {
            _logger.LogInfo("═══════════════════════════════════════════════════════════");
            _logger.LogInfo("         DETAILED PROCESSING STATISTICS                   ");
            _logger.LogInfo("═══════════════════════════════════════════════════════════");

            foreach (var kvp in statsByType.OrderByDescending(x => x.Value.TotalFiles))
            {
                var fileType = kvp.Key;
                var stats = kvp.Value;

                _logger.LogInfo($"\n{fileType}:");
                _logger.LogInfo($"  Total:           {stats.TotalFiles}");
                _logger.LogInfo($"  Successful:      {stats.SuccessCount} ({CalculatePercentage(stats.SuccessCount, stats.TotalFiles):F1}%)");
                _logger.LogInfo($"  Failed:          {stats.FailedCount}");
                _logger.LogInfo($"  Avg Duration:    {stats.AverageDuration.TotalSeconds:F2}s");
                _logger.LogInfo($"  Total Records:   {stats.TotalRecordsProcessed:N0}");
            }

            _logger.LogInfo("\n═══════════════════════════════════════════════════════════");
        }

        // ═══════════════════════════════════════════════════════════════════
        // KPI EVENTS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Log khi KPI được update
        /// </summary>
        /// <param name="kpiName">Name of KPI (e.g., "Total SKUs")</param>
        /// <param name="oldValue">Previous value</param>
        /// <param name="newValue">New value</param>
        public void LogKpiUpdate(string kpiName, string oldValue, string newValue)
        {
            _logger.LogInfo($"📊 KPI Updated: {kpiName} | {oldValue} → {newValue}");
        }

        /// <summary>
        /// Log KPI update with numeric values
        /// </summary>
        public void LogKpiUpdate(string kpiName, double oldValue, double newValue)
        {
            var change = newValue - oldValue;
            var changePercent = oldValue != 0 ? (change / oldValue * 100) : 0;
            var arrow = change > 0 ? "↑" : change < 0 ? "↓" : "→";

            _logger.LogInfo($"📊 KPI Updated: {kpiName} | {oldValue:F2} {arrow} {newValue:F2} ({changePercent:+0.0;-0.0}%)");
        }

        /// <summary>
        /// Log multiple KPI updates at once
        /// </summary>
        public void LogKpiUpdates(Dictionary<string, (double oldValue, double newValue)> updates)
        {
            if (updates == null || updates.Count == 0)
                return;

            _logger.LogInfo("📊 KPI Batch Update:");
            foreach (var kvp in updates)
            {
                var change = kvp.Value.newValue - kvp.Value.oldValue;
                var arrow = change > 0 ? "↑" : change < 0 ? "↓" : "→";
                _logger.LogInfo($"   {kvp.Key}: {kvp.Value.oldValue:F2} {arrow} {kvp.Value.newValue:F2}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // SYSTEM LIFECYCLE
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Log system startup với beautiful banner
        /// </summary>
        public void LogSystemStartup()
        {
            _logger.LogInfo("═════════════════════════════════════════════════════");
            _logger.LogInfo("      INVENTORY KPI SYSTEM STARTED");
            _logger.LogInfo($"      {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logger.LogInfo("═════════════════════════════════════════════════════");
        }

        /// <summary>
        /// Log system startup with version info
        /// </summary>
        public void LogSystemStartup(string version, string environment)
        {
            _logger.LogInfo("═════════════════════════════════════════════════════");
            _logger.LogInfo("      INVENTORY KPI SYSTEM STARTED");
            _logger.LogInfo($"      Version: {version}");
            _logger.LogInfo($"      Environment: {environment}");
            _logger.LogInfo($"      Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logger.LogInfo("═════════════════════════════════════════════════════");
        }

        /// <summary>
        /// Log system shutdown
        /// </summary>
        public void LogSystemShutdown()
        {
            _logger.LogInfo("═════════════════════════════════════════════════════");
            _logger.LogInfo("      SYSTEM SHUTTING DOWN");
            _logger.LogInfo($"      {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logger.LogInfo("═════════════════════════════════════════════════════");
        }

        /// <summary>
        /// Log system shutdown with uptime
        /// </summary>
        public void LogSystemShutdown(TimeSpan uptime)
        {
            _logger.LogInfo("═════════════════════════════════════════════════════");
            _logger.LogInfo("      SYSTEM SHUTTING DOWN");
            _logger.LogInfo($"      Uptime: {uptime:hh\\:mm\\:ss}");
            _logger.LogInfo($"      Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logger.LogInfo("═════════════════════════════════════════════════════");
        }

        // ═══════════════════════════════════════════════════════════════════
        // MONITORING & HEALTH
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Log memory usage hiện tại
        /// </summary>
        public void LogMemoryUsage()
        {
            var memoryMB = GC.GetTotalMemory(false) / 1024.0 / 1024.0;
            _logger.LogDebug($"💾 Memory: {memoryMB:F2} MB");
        }

        /// <summary>
        /// Log detailed memory usage
        /// </summary>
        public void LogDetailedMemoryUsage()
        {
            var totalMemory = GC.GetTotalMemory(false);
            var gen0 = GC.CollectionCount(0);
            var gen1 = GC.CollectionCount(1);
            var gen2 = GC.CollectionCount(2);

            _logger.LogDebug("💾 Memory Status:");
            _logger.LogDebug($"   Total: {totalMemory / 1024.0 / 1024.0:F2} MB");
            _logger.LogDebug($"   GC Gen0: {gen0} collections");
            _logger.LogDebug($"   GC Gen1: {gen1} collections");
            _logger.LogDebug($"   GC Gen2: {gen2} collections");
        }

        /// <summary>
        /// Log queue status
        /// </summary>
        /// <param name="queuedFiles">Number of files waiting to be processed</param>
        /// <param name="processingFiles">Number of files currently being processed</param>
        public void LogQueueStatus(int queuedFiles, int processingFiles)
        {
            _logger.LogDebug($"📋 Queue: {queuedFiles} queued, {processingFiles} processing");
        }

        /// <summary>
        /// Log detailed queue status
        /// </summary>
        public void LogDetailedQueueStatus(int queuedFiles, int processingFiles, int completedFiles, int failedFiles)
        {
            _logger.LogDebug("📋 Queue Status:");
            _logger.LogDebug($"   Queued:      {queuedFiles}");
            _logger.LogDebug($"   Processing:  {processingFiles}");
            _logger.LogDebug($"   Completed:   {completedFiles}");
            _logger.LogDebug($"   Failed:      {failedFiles}");
            _logger.LogDebug($"   Total:       {queuedFiles + processingFiles + completedFiles + failedFiles}");
        }

        /// <summary>
        /// Log health check result
        /// </summary>
        public void LogHealthCheck(bool isHealthy, string details = "")
        {
            if (isHealthy)
            {
                _logger.LogInfo($"💚 Health Check: PASSED {details}");
            }
            else
            {
                _logger.LogWarning($"💛 Health Check: DEGRADED {details}");
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // PROGRESS TRACKING
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Log progress update
        /// </summary>
        /// <param name="current">Current progress</param>
        /// <param name="total">Total items</param>
        /// <param name="description">What is being processed</param>
        public void LogProgress(int current, int total, string description)
        {
            var percentage = total > 0 ? (current * 100.0 / total) : 0;
            _logger.LogInfo($"⏳ Progress: {current}/{total} ({percentage:F1}%) - {description}");
        }

        /// <summary>
        /// Log progress with ETA
        /// </summary>
        public void LogProgressWithEta(int current, int total, TimeSpan elapsed, string description)
        {
            var percentage = total > 0 ? (current * 100.0 / total) : 0;
            var rate = elapsed.TotalSeconds > 0 ? current / elapsed.TotalSeconds : 0;
            var remaining = total - current;
            var eta = rate > 0 ? TimeSpan.FromSeconds(remaining / rate) : TimeSpan.Zero;

            _logger.LogInfo($"⏳ Progress: {current}/{total} ({percentage:F1}%) | ETA: {eta:mm\\:ss} | {description}");
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Calculate percentage safely
        /// </summary>
        private double CalculatePercentage(int part, int total)
        {
            return total > 0 ? (part * 100.0 / total) : 0;
        }

        /// <summary>
        /// Get current active timers count
        /// </summary>
        public int GetActiveTimersCount()
        {
            lock (_lockObject)
            {
                return _timers.Count;
            }
        }

        /// <summary>
        /// Clear all active timers
        /// </summary>
        public void ClearTimers()
        {
            lock (_lockObject)
            {
                foreach (var timer in _timers.Values)
                {
                    timer.Stop();
                }
                _timers.Clear();
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // SUPPORTING CLASSES
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Statistics for a specific file type
    /// </summary>
    public class FileTypeStats
    {
        public int TotalFiles { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public long TotalRecordsProcessed { get; set; }

        public TimeSpan AverageDuration => TotalFiles > 0
            ? TimeSpan.FromSeconds(TotalDuration.TotalSeconds / TotalFiles)
            : TimeSpan.Zero;

        public double SuccessRate => TotalFiles > 0
            ? (SuccessCount * 100.0 / TotalFiles)
            : 0;
    }
}