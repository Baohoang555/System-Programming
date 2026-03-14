using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InventoryKpiSystem.Infrastructure.Persistence;

namespace InventoryKpiSystem.Tests.IntegrationTests
{
    [TestClass]
    public class ConcurrentProcessingTests
    {
        private string _testDirectory;

        [TestInitialize]
        public void Setup()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "ConcurrentTests", Guid.NewGuid().ToString());
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
        public async Task TestConcurrentFileTracking()
        {
            // Arrange
            var tracker = new FileTracker(_testDirectory);
            const int fileCount = 100;
            const int threadsCount = 10;

            // Create test files
            var testFiles = new List<string>();
            for (int i = 0; i < fileCount; i++)
            {
                var filePath = Path.Combine(_testDirectory, $"concurrent_test_{i}.json");
                File.WriteAllText(filePath, $"{{\"id\": {i}, \"data\": \"test data {i}\"}}");
                testFiles.Add(filePath);
            }

            // Act - Process files concurrently
            var tasks = new List<Task>();
            var filesPerThread = fileCount / threadsCount;

            for (int t = 0; t < threadsCount; t++)
            {
                var threadId = t;
                var startIndex = threadId * filesPerThread;
                var endIndex = (threadId == threadsCount - 1) ? fileCount : startIndex + filesPerThread;

                tasks.Add(Task.Run(() =>
                {
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        var file = testFiles[i];

                        // Check if processed (should be false initially)
                        var isProcessed = tracker.IsFileProcessed(file);
                        Assert.IsFalse(isProcessed, $"File {i} should not be processed yet");

                        // Mark as processed
                        tracker.MarkAsProcessed(file);

                        // Verify it's now processed
                        isProcessed = tracker.IsFileProcessed(file);
                        Assert.IsTrue(isProcessed, $"File {i} should be processed now");
                    }
                }));
            }

            await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(fileCount, tracker.GetProcessedFileCount(),
                "All files should be marked as processed");

            // Verify no duplicates by checking again
            foreach (var file in testFiles)
            {
                Assert.IsTrue(tracker.IsFileProcessed(file),
                    $"File {file} should still be marked as processed");
            }
        }

        [TestMethod]
        public async Task TestConcurrentKpiReportSaving()
        {
            // Arrange
            var registry = new KpiRegistry(_testDirectory);
            const int reportsCount = 50;

            // Act - Save reports concurrently
            var tasks = new List<Task>();
            for (int i = 0; i < reportsCount; i++)
            {
                var reportId = i;
                tasks.Add(Task.Run(async () =>
                {
                    var report = new KpiReport
                    {
                        TotalSKUs = 100 + reportId,
                        CostOfInventory = 50000 + reportId * 100,
                        OutOfStockItems = reportId % 10,
                        AverageDailySales = 150.5 + reportId,
                        AverageInventoryAge = 30.2 + reportId * 0.5,
                        GeneratedAt = DateTime.Now
                    };

                    await registry.SaveReportAsync(report);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - Check that all reports were saved
            var reportFiles = Directory.GetFiles(_testDirectory, "kpi-report-*.json");
            Assert.AreEqual(reportsCount, reportFiles.Length,
                "All reports should be saved");
        }

        [TestMethod]
        public void TestThreadSafetyOfFileTracker()
        {
            // Arrange
            var tracker = new FileTracker(_testDirectory);
            var testFile = Path.Combine(_testDirectory, "thread_safety_test.json");
            File.WriteAllText(testFile, "{\"test\": true}");

            const int attempts = 1000;
            var successCount = 0;
            var lockObject = new object();

            // Act - Try to mark the same file as processed from multiple threads
            var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < attempts / 10; i++)
                {
                    if (!tracker.IsFileProcessed(testFile))
                    {
                        tracker.MarkAsProcessed(testFile);
                        lock (lockObject)
                        {
                            successCount++;
                        }
                    }
                }
            })).ToArray();

            Task.WaitAll(tasks);

            // Assert - File should only be marked once despite multiple attempts
            Assert.AreEqual(1, tracker.GetProcessedFileCount(),
                "File should only be tracked once");
            Assert.IsTrue(tracker.IsFileProcessed(testFile),
                "File should be marked as processed");
        }
    }
}