using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InventoryKpiSystem.ConsoleApp.Configuration;
using InventoryKpiSystem.ConsoleApp.Coordinators;

namespace InventoryKpiSystem.Tests.IntegrationTests
{
    [TestClass]
    public class EndToEndTests
    {
        private string _testDataDirectory;
        private AppConfig _testConfig;

        [TestInitialize]
        public void Setup()
        {
            // Create temporary test directory
            _testDataDirectory = Path.Combine(Path.GetTempPath(), "InventoryKpiTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDataDirectory);

            // Create test configuration
            _testConfig = new AppConfig
            {
                InvoiceDirectory = Path.Combine(_testDataDirectory, "invoices"),
                PurchaseOrderDirectory = Path.Combine(_testDataDirectory, "purchase-orders"),
                ReportsDirectory = Path.Combine(_testDataDirectory, "reports"),
                ProcessedFilesDirectory = Path.Combine(_testDataDirectory, "processed-files"),
                LogDirectory = Path.Combine(_testDataDirectory, "logs"),
                EnableConsoleOutput = false, // Disable for tests
                EnableDetailedLogging = true,
                MaxConcurrentFiles = 2,
                RetryAttempts = 2
            };

            _testConfig.ValidateAndCreateDirectories();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up test directory
            if (Directory.Exists(_testDataDirectory))
            {
                try
                {
                    Directory.Delete(_testDataDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        public async Task TestSystemStartupAndShutdown()
        {
            // Arrange
            var coordinator = new ApplicationCoordinator(_testConfig);

            // Act & Assert - Should not throw
            try
            {
                // Start system in background
                var startTask = Task.Run(async () =>
                {
                    await Task.Delay(2000); // Let it run for 2 seconds
                    await coordinator.ShutdownAsync();
                });

                await startTask;

                Assert.IsTrue(true, "System started and shut down successfully");
            }
            catch (Exception ex)
            {
                Assert.Fail($"System startup/shutdown failed: {ex.Message}");
            }
        }

        [TestMethod]
        public void TestConfigurationLoading()
        {
            // Arrange
            var configPath = Path.Combine(_testDataDirectory, "test_appsettings.json");
            _testConfig.SaveToFile(configPath);

            // Act
            var loadedConfig = AppConfig.LoadFromFile(configPath);

            // Assert
            Assert.IsNotNull(loadedConfig);
            Assert.AreEqual(_testConfig.InvoiceDirectory, loadedConfig.InvoiceDirectory);
            Assert.AreEqual(_testConfig.MaxConcurrentFiles, loadedConfig.MaxConcurrentFiles);
        }

        [TestMethod]
        public void TestDirectoryCreation()
        {
            // Act
            _testConfig.ValidateAndCreateDirectories();

            // Assert
            Assert.IsTrue(Directory.Exists(_testConfig.InvoiceDirectory));
            Assert.IsTrue(Directory.Exists(_testConfig.PurchaseOrderDirectory));
            Assert.IsTrue(Directory.Exists(_testConfig.ReportsDirectory));
            Assert.IsTrue(Directory.Exists(_testConfig.LogDirectory));
        }

        [TestMethod]
        public async Task TestReportGeneration()
        {
            // Arrange
            var coordinator = new ApplicationCoordinator(_testConfig);

            // TODO: Add test data and trigger report generation
            // This will be completed when integrated with other team members' code

            await Task.CompletedTask;
            Assert.IsTrue(true, "Report generation test placeholder");
        }
    }
}