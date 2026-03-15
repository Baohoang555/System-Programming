using System;
using System.Collections.Generic;
using System.Linq;                    
using InventoryKpiSystem.Core.Models;
using InventoryKpiSystem.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace InventoryKpiSystem.Tests.UnitTests
{
    // ====================================================
    // Dùng MSTest framework (built-in với .NET)
    // Mỗi [TestMethod] = 1 kịch bản cụ thể
    // Pattern: Arrange → Act → Assert
    //   Arrange = chuẩn bị dữ liệu
    //   Act     = gọi method cần test
    //   Assert  = kiểm tra kết quả đúng không
    // ====================================================
    [TestClass]
    public class KpiLogicTests
    {
        // ====================================================
        // HELPER: Tạo dữ liệu mẫu dùng chung cho nhiều test
        // Tránh lặp code ở từng test method
        // ====================================================
        private static List<PurchaseOrder> CreateSampleOrders() => new()
        {
            new PurchaseOrder
            {
                OrderId   = "O001",
                ProductId = "P001",
                Quantity  = 100,
                UnitCost  = 50_000m,
                Date      = new DateTime(2025, 1, 1)
            },
            new PurchaseOrder
            {
                OrderId   = "O002",
                ProductId = "P001",
                Quantity  = 50,
                UnitCost  = 50_000m,
                Date      = new DateTime(2025, 1, 15)
            },
            new PurchaseOrder
            {
                OrderId   = "O003",
                ProductId = "P002",
                Quantity  = 200,
                UnitCost  = 30_000m,
                Date      = new DateTime(2025, 1, 5)
            }
        };

        private static List<Invoice> CreateSampleInvoices() => new()
        {
            new Invoice
            {
                InvoiceId = "I001",
                ProductId = "P001",
                Quantity  = 80,
                UnitPrice = 70_000m,
                Date      = new DateTime(2025, 2, 1)
            },
            new Invoice
            {
                InvoiceId = "I002",
                ProductId = "P001",
                Quantity  = 20,
                UnitPrice = 70_000m,
                Date      = new DateTime(2025, 2, 3)
            },
            new Invoice
            {
                InvoiceId = "I003",
                ProductId = "P002",
                Quantity  = 200,   // bán hết → out of stock
                UnitPrice = 45_000m,
                Date      = new DateTime(2025, 2, 5)
            }
        };

        // ============================================================
        // PHẦN 1: TEST IncrementalKpiUpdater
        // ============================================================

        [TestMethod]
        public void ProcessNewPurchaseOrders_ShouldAccumulateCorrectly()
        {
            // Arrange
            var updater = new IncrementalKpiUpdater();
            var orders = CreateSampleOrders();

            // Act
            updater.ProcessNewPurchaseOrders(orders);

            // Assert — P001: 100 + 50 = 150
            var p001 = updater.GetAggregate("P001");
            Assert.IsNotNull(p001);
            Assert.AreEqual(150, p001.TotalPurchased);
            Assert.AreEqual(7_500_000m, p001.TotalPurchaseCost); // 150 * 50,000

            // Assert — P002: 200
            var p002 = updater.GetAggregate("P002");
            Assert.IsNotNull(p002);
            Assert.AreEqual(200, p002.TotalPurchased);
        }

        [TestMethod]
        public void ProcessNewPurchaseOrders_ShouldTrackEarliestAndLatestDate()
        {
            // Arrange
            var updater = new IncrementalKpiUpdater();
            var orders = CreateSampleOrders();

            // Act
            updater.ProcessNewPurchaseOrders(orders);

            // Assert — P001 có 2 order: 1/1 và 15/1
            var p001 = updater.GetAggregate("P001");
            Assert.AreEqual(new DateTime(2025, 1, 1), p001!.EarliestPurchaseDate);
            Assert.AreEqual(new DateTime(2025, 1, 15), p001.LatestPurchaseDate);
        }

        [TestMethod]
        public void ProcessNewInvoices_ShouldAccumulateTotalSold()
        {
            // Arrange
            var updater = new IncrementalKpiUpdater();
            var invoices = CreateSampleInvoices();

            // Act
            updater.ProcessNewInvoices(invoices);

            // Assert — P001: 80 + 20 = 100
            var p001 = updater.GetAggregate("P001");
            Assert.AreEqual(100, p001!.TotalSold);

            // Assert — P002: 200
            var p002 = updater.GetAggregate("P002");
            Assert.AreEqual(200, p002!.TotalSold);
        }

        [TestMethod]
        public void ProcessNewInvoices_ShouldTrackUniqueSaleDates()
        {
            // Arrange
            var updater = new IncrementalKpiUpdater();

            // 3 invoices cùng ngày 1/2 → SaleDates chỉ đếm 1 lần
            var invoices = new List<Invoice>
            {
                new() { InvoiceId="I1", ProductId="P001", Quantity=10, Date=new DateTime(2025,2,1) },
                new() { InvoiceId="I2", ProductId="P001", Quantity=20, Date=new DateTime(2025,2,1) },
                new() { InvoiceId="I3", ProductId="P001", Quantity=15, Date=new DateTime(2025,2,3) },
            };

            // Act
            updater.ProcessNewInvoices(invoices);

            // Assert — chỉ có 2 ngày phân biệt: 1/2 và 3/2
            var p001 = updater.GetAggregate("P001");
            Assert.AreEqual(2, p001!.SaleDates.Count);
        }
        
        [TestMethod]
        public void ProcessOrders_ThenInvoices_ShouldMergeCorrectly()
        {
            // Arrange — test cả 2 loại dữ liệu cộng vào cùng 1 aggregate
            var updater = new IncrementalKpiUpdater();

            // Act
            updater.ProcessNewPurchaseOrders(CreateSampleOrders());
            updater.ProcessNewInvoices(CreateSampleInvoices());

            // Assert — P001: mua 150, bán 100 → còn 50
            var p001 = updater.GetAggregate("P001");
            Assert.AreEqual(150, p001!.TotalPurchased);
            Assert.AreEqual(100, p001.TotalSold);
        }

        [TestMethod]
        public void RestoreFromSnapshot_ShouldRestoreCorrectly()
        {
            // Arrange
            var updater = new IncrementalKpiUpdater();
            var snapshot = new Dictionary<string, ProductAggregate>
            {
                ["P001"] = new ProductAggregate
                {
                    TotalPurchased = 500,
                    TotalSold = 300
                }
            };

            // Act
            updater.RestoreFromSnapshot(snapshot);

            // Assert
            var p001 = updater.GetAggregate("P001");
            Assert.AreEqual(500, p001!.TotalPurchased);
            Assert.AreEqual(300, p001.TotalSold);
        }

        // ============================================================
        // PHẦN 2: TEST KpiCalculator
        // ============================================================

        private IReadOnlyDictionary<string, ProductAggregate> BuildAggregates()
        {
            // Tạo aggregates trực tiếp thay vì qua updater
            // → test KpiCalculator độc lập, không phụ thuộc IncrementalKpiUpdater
            var updater = new IncrementalKpiUpdater();
            updater.ProcessNewPurchaseOrders(CreateSampleOrders());
            updater.ProcessNewInvoices(CreateSampleInvoices());
            return updater.GetAllAggregates();
        }

        [TestMethod]
        public void CalculatePerSkuKpis_StockValue_ShouldBeCorrect()
        {
            // Arrange
            // P001: mua 150 (cost 50,000/cái), bán 100 → còn 50
            // AvgUnitCost = 7,500,000 / 150 = 50,000
            // StockValue  = 50 * 50,000 = 2,500,000
            var calculator = new KpiCalculator();
            var aggregates = BuildAggregates();

            // Act
            var results = calculator.CalculatePerSkuKpis(aggregates);
            var p001 = results.First(k => k.ProductId == "P001");

            // Assert
            Assert.AreEqual(2_500_000m, p001.StockValue);
        }

        [TestMethod]
        public void CalculatePerSkuKpis_OutOfStock_ShouldDetectCorrectly()
        {
            // Arrange — P002: mua 200, bán 200 → hết hàng
            var calculator = new KpiCalculator();
            var aggregates = BuildAggregates();

            // Act
            var results = calculator.CalculatePerSkuKpis(aggregates);
            var p001 = results.First(k => k.ProductId == "P001");
            var p002 = results.First(k => k.ProductId == "P002");

            // Assert
            Assert.IsFalse(p001.IsOutOfStock);  // P001 còn 50 → không hết
            Assert.IsTrue(p002.IsOutOfStock);   // P002 còn 0  → hết hàng
        }

        [TestMethod]
        public void CalculatePerSkuKpis_AvgDailySales_ShouldBeCorrect()
        {
            // Arrange — P001: bán 100 trong 2 ngày (1/2 và 3/2)
            // AvgDailySales = 100 / 2 = 50
            var calculator = new KpiCalculator();
            var aggregates = BuildAggregates();

            // Act
            var results = calculator.CalculatePerSkuKpis(aggregates);
            var p001 = results.First(k => k.ProductId == "P001");

            // Assert
            Assert.AreEqual(50m, p001.AvgDailySales);
        }

        [TestMethod]
        public void CalculateSystemWideKpis_TotalSkus_ShouldCountAllProducts()
        {
            // Arrange — có P001 và P002 → TotalSkus = 2
            var calculator = new KpiCalculator();
            var aggregates = BuildAggregates();
            var perSku = calculator.CalculatePerSkuKpis(aggregates);

            // Act
            var systemWide = calculator.CalculateSystemWideKpis(perSku, aggregates);

            // Assert
            Assert.AreEqual(2, systemWide.TotalSkus);
        }

        [TestMethod]
        public void CalculateSystemWideKpis_OutOfStockCount_ShouldBeCorrect()
        {
            // Arrange — chỉ P002 hết hàng → OutOfStockCount = 1
            var calculator = new KpiCalculator();
            var aggregates = BuildAggregates();
            var perSku = calculator.CalculatePerSkuKpis(aggregates);

            // Act
            var systemWide = calculator.CalculateSystemWideKpis(perSku, aggregates);

            // Assert
            Assert.AreEqual(1, systemWide.OutOfStockCount);
        }

        // ============================================================
        // PHẦN 3: TEST EDGE CASES — dữ liệu bất thường
        // ============================================================

        [TestMethod]
        public void ProcessNewPurchaseOrders_EmptyList_ShouldNotThrow()
        {
            // Arrange
            var updater = new IncrementalKpiUpdater();

            // Act & Assert — không được throw exception khi list rỗng
            updater.ProcessNewPurchaseOrders(new List<PurchaseOrder>());
            Assert.AreEqual(0, updater.GetAllAggregates().Count);
        }

        [TestMethod]
        public void CalculatePerSkuKpis_NoSalesData_AvgDailySales_ShouldBeZero()
        {
            // Arrange — chỉ có purchase orders, không có invoice
            var updater = new IncrementalKpiUpdater();
            updater.ProcessNewPurchaseOrders(CreateSampleOrders());

            var calculator = new KpiCalculator();

            // Act
            var results = calculator.CalculatePerSkuKpis(updater.GetAllAggregates());
            var p001 = results.First(k => k.ProductId == "P001");

            // Assert — không có ngày bán → AvgDailySales = 0, không crash
            Assert.AreEqual(0m, p001.AvgDailySales);
        }

        [TestMethod]
        public void CalculatePerSkuKpis_SoldMoreThanPurchased_CurrentStock_ShouldBeNegative()
        {
            // Arrange — edge case: bán nhiều hơn mua (dữ liệu lỗi)
            var updater = new IncrementalKpiUpdater();
            updater.ProcessNewPurchaseOrders(new List<PurchaseOrder>
            {
                new() { OrderId="O1", ProductId="P001", Quantity=10, UnitCost=100m, Date=DateTime.Today }
            });
            updater.ProcessNewInvoices(new List<Invoice>
            {
                new() { InvoiceId="I1", ProductId="P001", Quantity=50, UnitPrice=150m, Date=DateTime.Today }
            });

            var calculator = new KpiCalculator();

            // Act
            var results = calculator.CalculatePerSkuKpis(updater.GetAllAggregates());
            var p001 = results.First(k => k.ProductId == "P001");

            // Assert — CurrentStock âm, IsOutOfStock = true, StockValue = 0 (không âm)
            Assert.IsTrue(p001.CurrentStock < 0);
            Assert.IsTrue(p001.IsOutOfStock);
            Assert.AreEqual(0m, p001.StockValue); // StockValue không được âm
        }

        [TestMethod]
        public void GenerateReport_ShouldReturnCorrectReportStructure()
        {
            // Arrange
            var updater = new IncrementalKpiUpdater();
            var calculator = new KpiCalculator();
            updater.ProcessNewPurchaseOrders(CreateSampleOrders());
            updater.ProcessNewInvoices(CreateSampleInvoices());

            // Act
            var report = calculator.GenerateReport(updater.GetAllAggregates());

            // Assert — report phải có đầy đủ các field
            Assert.IsFalse(string.IsNullOrEmpty(report.ReportId));
            Assert.AreEqual(2, report.Details.Count);
            Assert.IsNotNull(report.SystemWide);
            Assert.AreEqual(2, report.TotalProductsProcessed);
        }
    }
}