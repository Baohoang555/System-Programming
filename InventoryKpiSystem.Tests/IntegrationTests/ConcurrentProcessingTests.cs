using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InventoryKpiSystem.Core.Models;
using InventoryKpiSystem.Infrastructure.Persistence;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace InventoryKpiSystem.Tests.IntegrationTests
{
    [TestClass]
    public class ConcurrentProcessingTests
    {
        private string _testDirectory = string.Empty; // fix CS8618 warning

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
                try { Directory.Delete(_testDirectory, true); }
                catch { }
            }
        }

        [TestMethod]
        public async Task TestConcurrentFileTracking()
        {
            var tracker = new FileTracker(_testDirectory);
            const int fileCount = 100;
            const int threadsCount = 10;

            var testFiles = new List<string>();
            for (int i = 0; i < fileCount; i++)
            {
                var filePath = Path.Combine(_testDirectory, $"concurrent_test_{i}.json");
                File.WriteAllText(filePath, $"{{\"id\": {i}, \"data\": \"test data {i}\"}}");
                testFiles.Add(filePath);
            }

            var tasks = new List<Task>();
            var filesPerThread = fileCount / threadsCount;

            for (int t = 0; t < threadsCount; t++)
            {
                var startIndex = t * filesPerThread;
                var endIndex = (t == threadsCount - 1) ? fileCount : startIndex + filesPerThread;

                tasks.Add(Task.Run(() =>
                {
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        var file = testFiles[i];
                        Assert.IsFalse(tracker.IsFileProcessed(file), $"File {i} should not be processed yet");
                        tracker.MarkAsProcessed(file);
                        Assert.IsTrue(tracker.IsFileProcessed(file), $"File {i} should be processed now");
                    }
                }));
            }

            await Task.WhenAll(tasks);

            Assert.AreEqual(fileCount, tracker.GetProcessedFileCount(), "All files should be marked as processed");
            foreach (var file in testFiles)
                Assert.IsTrue(tracker.IsFileProcessed(file), $"{file} should still be marked");
        }

        [TestMethod]
        public async Task TestConcurrentKpiReportSaving()
        {
            var registry = new KpiRegistry(_testDirectory);
            const int reportsCount = 50;

            var tasks = new List<Task>();
            for (int i = 0; i < reportsCount; i++)
            {
                var reportId = i;
                tasks.Add(Task.Run(async () =>
                {
                    // Use Core.Models.KpiReport — correct property names
                    var report = new KpiReport
                    {
                        ReportId = Guid.NewGuid().ToString(),
                        ExportedDate = DateTime.Now,
                        TotalProductsProcessed = 100 + reportId,
                        TotalStockValue = 50000 + reportId * 100,
                        SystemWide = new SystemWideKpi
                        {
                            OutOfStockCount = reportId % 10,
                            AvgDailySales = 150.5m + reportId,
                            AvgInventoryAgeDays = 30.2 + reportId * 0.5
                        }
                    };
                    await registry.SaveReportAsync(report);
                }));
            }

            await Task.WhenAll(tasks);

            var reportFiles = Directory.GetFiles(_testDirectory, "kpi-report-*.json");
            Assert.AreEqual(reportsCount, reportFiles.Length, "All reports should be saved");
        }

        [TestMethod]
        public void TestThreadSafetyOfFileTracker()
        {
            var tracker = new FileTracker(_testDirectory);
            var testFile = Path.Combine(_testDirectory, "thread_safety_test.json");
            File.WriteAllText(testFile, "{\"test\": true}");

            var successCount = 0;
            var lockObject = new object();

            var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    if (!tracker.IsFileProcessed(testFile))
                    {
                        tracker.MarkAsProcessed(testFile);
                        lock (lockObject) { successCount++; }
                    }
                }
            })).ToArray();

            Task.WaitAll(tasks);

            Assert.AreEqual(1, tracker.GetProcessedFileCount(), "File should only be tracked once");
            Assert.IsTrue(tracker.IsFileProcessed(testFile), "File should be marked as processed");
        }
    }
}