using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InventoryKpiSystem.Core.Models;
using InventoryKpiSystem.Infrastructure.Logging;
using InventoryKpiSystem.Infrastructure.Persistence;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace InventoryKpiSystem.Tests.IntegrationTests
{
    [TestClass]
    public class PerformanceTests
    {
        private string _testDirectory = string.Empty; // fix CS8618 warning

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "PerfTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDirectory))
            {
                try { Directory.Delete(_testDirectory, true); }
                catch { }
            }
        }

        [TestMethod]
        public void TestLoggingPerformance()
        {
            var logger = new Logger(_testDirectory, "perf_test.log", enableConsoleOutput: false);
            var stopwatch = Stopwatch.StartNew();
            const int iterations = 10000;

            for (int i = 0; i < iterations; i++)
                logger.LogInfo($"Performance test message {i}");

            stopwatch.Stop();

            var timePerLog = stopwatch.ElapsedMilliseconds / (double)iterations;
            Assert.IsTrue(timePerLog < 1.0, $"Logging too slow: {timePerLog:F2}ms per log (should be < 1ms)");
            Console.WriteLine($"Logging Performance: {timePerLog:F3}ms per log | Total: {stopwatch.ElapsedMilliseconds}ms");
        }

        [TestMethod]
        public void TestFileTrackerPerformance()
        {
            var tracker = new FileTracker(_testDirectory);
            var testFiles = new string[1000];

            for (int i = 0; i < testFiles.Length; i++)
            {
                var filePath = Path.Combine(_testDirectory, $"test_{i}.json");
                File.WriteAllText(filePath, $"{{\"id\": {i}}}");
                testFiles[i] = filePath;
            }

            var stopwatch = Stopwatch.StartNew();
            foreach (var file in testFiles)
                tracker.MarkAsProcessed(file);
            stopwatch.Stop();

            var timePerFile = stopwatch.ElapsedMilliseconds / (double)testFiles.Length;
            Assert.IsTrue(timePerFile < 50.0, $"File tracking too slow: {timePerFile:F2}ms per file");
            Console.WriteLine($"File Tracking: {timePerFile:F3}ms per file | Total: {stopwatch.ElapsedMilliseconds}ms");
        }

        [TestMethod]
        public async Task TestKpiRegistryPerformance()
        {
            var registry = new KpiRegistry(_testDirectory);

            // Use Core.Models.KpiReport — correct property names
            var testReport = new KpiReport
            {
                ReportId = Guid.NewGuid().ToString(),
                ExportedDate = DateTime.Now,
                TotalProductsProcessed = 100,
                TotalStockValue = 50000m,
                SystemWide = new SystemWideKpi
                {
                    TotalSkus = 100,
                    OutOfStockCount = 5,
                    AvgDailySales = 150.5m,
                    AvgInventoryAgeDays = 30.2
                }
            };

            var stopwatch = Stopwatch.StartNew();
            const int iterations = 100;

            for (int i = 0; i < iterations; i++)
                await registry.SaveReportAsync(testReport);

            stopwatch.Stop();

            var timePerSave = stopwatch.ElapsedMilliseconds / (double)iterations;
            Assert.IsTrue(timePerSave < 50.0, $"Report saving too slow: {timePerSave:F2}ms per save");
            Console.WriteLine($"Report Save: {timePerSave:F2}ms per save | Total: {stopwatch.ElapsedMilliseconds}ms");
        }

        [TestMethod]
        public void TestMemoryUsage()
        {
            var initialMemory = GC.GetTotalMemory(true) / 1024.0 / 1024.0;
            var logger = new Logger(_testDirectory, enableFileOutput: false);

            for (int i = 0; i < 100000; i++)
                logger.LogInfo($"Memory test {i}");

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(true) / 1024.0 / 1024.0;
            var memoryIncrease = finalMemory - initialMemory;

            Assert.IsTrue(memoryIncrease < 50, $"Memory too high: {memoryIncrease:F2} MB increase");
            Console.WriteLine($"Memory: {initialMemory:F2} MB → {finalMemory:F2} MB (+{memoryIncrease:F2} MB)");
        }

        [TestMethod]
        public void TestConcurrentLogging()
        {
            var logger = new Logger(_testDirectory, enableConsoleOutput: false);
            var stopwatch = Stopwatch.StartNew();
            const int threadsCount = 10;
            const int logsPerThread = 1000;

            var tasks = new Task[threadsCount];
            for (int t = 0; t < threadsCount; t++)
            {
                var threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < logsPerThread; i++)
                        logger.LogInfo($"Thread {threadId} - Message {i}");
                });
            }

            Task.WaitAll(tasks);
            stopwatch.Stop();

            var totalLogs = threadsCount * logsPerThread;
            var logsPerSecond = totalLogs / stopwatch.Elapsed.TotalSeconds;

            Assert.IsTrue(logsPerSecond > 1000, $"Concurrent logging too slow: {logsPerSecond:F0} logs/sec");
            Console.WriteLine($"Concurrent: {logsPerSecond:N0} logs/sec | {totalLogs:N0} logs in {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}