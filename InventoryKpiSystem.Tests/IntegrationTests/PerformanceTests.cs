using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InventoryKpiSystem.Infrastructure.Logging;
using InventoryKpiSystem.Infrastructure.Persistence;

namespace InventoryKpiSystem.Tests.IntegrationTests
{
    [TestClass]
    public class PerformanceTests
    {
        private string _testDirectory;

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
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch { }
            }
        }

        [TestMethod]
        public void TestLoggingPerformance()
        {
            // Arrange
            var logger = new Logger(_testDirectory, "perf_test.log", enableConsoleOutput: false);
            var stopwatch = Stopwatch.StartNew();
            const int iterations = 10000;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                logger.LogInfo($"Performance test message {i}");
            }
            stopwatch.Stop();

            // Assert
            var timePerLog = stopwatch.ElapsedMilliseconds / (double)iterations;
            Assert.IsTrue(timePerLog < 1.0, $"Logging too slow: {timePerLog:F2}ms per log (should be < 1ms)");

            Console.WriteLine($"Logging Performance: {timePerLog:F3}ms per log");
            Console.WriteLine($"Total: {stopwatch.ElapsedMilliseconds}ms for {iterations} logs");
        }

        [TestMethod]
        public void TestFileTrackerPerformance()
        {
            // Arrange
            var tracker = new FileTracker(_testDirectory);
            var testFiles = new string[1000];

            // Create test files
            for (int i = 0; i < testFiles.Length; i++)
            {
                var filePath = Path.Combine(_testDirectory, $"test_{i}.json");
                File.WriteAllText(filePath, $"{{\"id\": {i}}}");
                testFiles[i] = filePath;
            }

            var stopwatch = Stopwatch.StartNew();

            // Act - Mark files as processed
            foreach (var file in testFiles)
            {
                tracker.MarkAsProcessed(file);
            }
            stopwatch.Stop();

            // Assert
            var timePerFile = stopwatch.ElapsedMilliseconds / (double)testFiles.Length;
            Assert.IsTrue(timePerFile < 5.0, $"File tracking too slow: {timePerFile:F2}ms per file");

            Console.WriteLine($"File Tracking Performance: {timePerFile:F3}ms per file");
            Console.WriteLine($"Total: {stopwatch.ElapsedMilliseconds}ms for {testFiles.Length} files");
        }

        [TestMethod]
        public async Task TestKpiRegistryPerformance()
        {
            // Arrange
            var registry = new KpiRegistry(_testDirectory);
            var testReport = new KpiReport
            {
                TotalSKUs = 100,
                CostOfInventory = 50000,
                OutOfStockItems = 5,
                AverageDailySales = 150.5,
                AverageInventoryAge = 30.2,
                GeneratedAt = DateTime.Now
            };

            var stopwatch = Stopwatch.StartNew();
            const int iterations = 100;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                await registry.SaveReportAsync(testReport);
            }
            stopwatch.Stop();

            // Assert
            var timePerSave = stopwatch.ElapsedMilliseconds / (double)iterations;
            Assert.IsTrue(timePerSave < 50.0, $"Report saving too slow: {timePerSave:F2}ms per save");

            Console.WriteLine($"Report Save Performance: {timePerSave:F2}ms per save");
            Console.WriteLine($"Total: {stopwatch.ElapsedMilliseconds}ms for {iterations} saves");
        }

        [TestMethod]
        public void TestMemoryUsage()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true) / 1024.0 / 1024.0; // MB
            var logger = new Logger(_testDirectory, enableFileOutput: false);

            // Act - Create many objects
            for (int i = 0; i < 100000; i++)
            {
                logger.LogInfo($"Memory test {i}");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(true) / 1024.0 / 1024.0; // MB
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            Assert.IsTrue(memoryIncrease < 50, $"Memory usage too high: {memoryIncrease:F2} MB increase");

            Console.WriteLine($"Initial Memory: {initialMemory:F2} MB");
            Console.WriteLine($"Final Memory: {finalMemory:F2} MB");
            Console.WriteLine($"Memory Increase: {memoryIncrease:F2} MB");
        }

        [TestMethod]
        public void TestConcurrentLogging()
        {
            // Arrange
            var logger = new Logger(_testDirectory, enableConsoleOutput: false);
            var stopwatch = Stopwatch.StartNew();
            const int threadsCount = 10;
            const int logsPerThread = 1000;

            // Act - Multiple threads logging simultaneously
            var tasks = new Task[threadsCount];
            for (int t = 0; t < threadsCount; t++)
            {
                var threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < logsPerThread; i++)
                    {
                        logger.LogInfo($"Thread {threadId} - Message {i}");
                    }
                });
            }

            Task.WaitAll(tasks);
            stopwatch.Stop();

            // Assert
            var totalLogs = threadsCount * logsPerThread;
            var logsPerSecond = totalLogs / stopwatch.Elapsed.TotalSeconds;

            Assert.IsTrue(logsPerSecond > 5000, $"Concurrent logging too slow: {logsPerSecond:F0} logs/sec");

            Console.WriteLine($"Concurrent Logging Performance:");
            Console.WriteLine($"  Threads: {threadsCount}");
            Console.WriteLine($"  Total Logs: {totalLogs:N0}");
            Console.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"  Throughput: {logsPerSecond:N0} logs/sec");
        }
    }
}