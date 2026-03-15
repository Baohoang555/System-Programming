using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InventoryKpiSystem.Infrastructure.Monitoring;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert; // resolve ambiguity with Xunit.Assert

namespace InventoryKpiSystem.Tests.UnitTests
{
    [TestClass]
    public class FileMonitoringTests
    {
        private string _watchDirectory = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _watchDirectory = Path.Combine(
                Path.GetTempPath(), "WatcherTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_watchDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_watchDirectory))
            {
                try { Directory.Delete(_watchDirectory, true); }
                catch { }
            }
        }

        [TestMethod]
        public async Task InventoryWatcher_DetectsNewJsonFile()
        {
            // Arrange
            var detected = new TaskCompletionSource<string>();
            var watcher = new InventoryWatcher(_watchDirectory);

            watcher.OnFileDetected += args =>
                detected.TrySetResult(args.FilePath);

            // Act — drop a new JSON file into the watched folder
            var testFile = Path.Combine(_watchDirectory, "new_invoice.json");
            await File.WriteAllTextAsync(testFile, "{\"invoiceId\": \"INV-999\"}");

            // Assert — event fires within 3 seconds
            var completedTask = await Task.WhenAny(detected.Task, Task.Delay(3000));
            Assert.IsTrue(completedTask == detected.Task,
                "InventoryWatcher did not raise OnFileDetected within 3 seconds");
            Assert.AreEqual(testFile, detected.Task.Result);
        }

        [TestMethod]
        public async Task InventoryWatcher_IgnoresNonJsonFiles()
        {
            // Arrange
            var detected = false;
            var watcher = new InventoryWatcher(_watchDirectory);
            watcher.OnFileDetected += _ => detected = true;

            // Act — drop a non-JSON file
            var txtFile = Path.Combine(_watchDirectory, "readme.txt");
            await File.WriteAllTextAsync(txtFile, "not json");

            // Wait briefly to confirm no event fires
            await Task.Delay(500);

            // Assert
            Assert.IsFalse(detected, "Watcher should not trigger for non-JSON files");
        }

        [TestMethod]
        public void FileEventArgs_ShouldCaptureCorrectPath()
        {
            // Arrange & Act
            var args = new FileEventArgs("/some/path/file.json");

            // Assert
            Assert.AreEqual("/some/path/file.json", args.FilePath);
            Assert.IsTrue(args.Time <= DateTime.Now);
            Assert.IsTrue(args.Time > DateTime.Now.AddSeconds(-5));
        }
    }
}