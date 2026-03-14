using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using InventoryKpiSystem.Core.Interfaces;
using InventoryKpiSystem.Core.Models;

namespace InventoryKpiSystem.Core.Services
{
    public class KpiCalculator
    {
        // The main method to calculate KPIs for a list of products
        public List<ProductKpi> CalculatePerSkuKpis(IReadOnlyDictionary<string, ProductAggregate> aggregates, DateTime? referenceDate = null)
        {
            // Dùng ngày tham chiếu để tính Inventory Age
            // Mặc định = hôm nay nếu không truyền vào
            var today = referenceDate ?? DateTime.Today;

            var results = new List<ProductKpi>();

            foreach (var kvp in aggregates)
            {
                var productId = kvp.Key;
                var agg = kvp.Value;

                // KPI 2: Stock Value 
                // Nếu âm → coi như 0 (không thể có kho âm)
                var unsoldQty = Math.Max(0, agg.TotalPurchased - agg.TotalSold);
                var avgUnitCost = agg.TotalPurchased > 0
                    ? agg.TotalPurchaseCost / agg.TotalPurchased   // Giá vốn bình quân
                    : 0m;
                var stockValue = unsoldQty * avgUnitCost;

                // KPI 3: Out-of-Stock 
                // true nếu ≤ 0
                var currentStock = agg.TotalPurchased - agg.TotalSold;
                var isOutOfStock = currentStock <= 0;

                // KPI 4: Average Daily Sales 
                var salesDaysCount = agg.SaleDates.Count;
                var avgDailySales = salesDaysCount > 0
                    ? (decimal)agg.TotalSold / salesDaysCount
                    : 0m;

                // KPI 5: Average Inventory Age 
                // Nếu chưa có giao dịch mua → Age = 0
                var inventoryAge = 0m;
                if (unsoldQty > 0 && agg.EarliestPurchaseDate != DateTime.MaxValue)
                {
                    // Lấy ngày mua sớm nhất của hàng còn tồn kho
                    var days = (today - agg.EarliestPurchaseDate.Date).Days;
                    inventoryAge = Math.Max(0, days); // Không cho âm
                }

                results.Add(new ProductKpi
                {
                    ProductId = productId,
                    CurrentStock = currentStock,
                    StockValue = Math.Round(stockValue, 2),
                    AvgDailySales = Math.Round(avgDailySales, 2),
                    InventoryAgeDays = (int)inventoryAge,
                    IsOutOfStock = isOutOfStock
                });
            }

            return results;
        }
        // The second method to calculate summary KPIs for the entire inventory
        // output sẽ là System-wide KPIs như tổng giá trị kho, tổng số sản phẩm, v.v.
        public SystemWideKpi CalculateSystemWideKpis(
       List<ProductKpi> perSkuKpis,
       IReadOnlyDictionary<string, ProductAggregate> aggregates)
        {
            // Nếu không có dữ liệu → trả về rỗng
            if (perSkuKpis.Count == 0)
                return new SystemWideKpi();

            // ── KPI 1: Total SKUs ─────────────────────────────
            // Đếm số sản phẩm phân biệt = số key trong aggregates
            var totalSkus = perSkuKpis.Count;

            // ── KPI 2: Total Stock Value (system-wide) ────────
            // Cộng StockValue của tất cả sản phẩm
            var totalStockValue = perSkuKpis.Sum(k => k.StockValue);

            // ── KPI 3: Out-of-Stock Items (system-wide) ───────
            // Đếm số sản phẩm có IsOutOfStock = true
            var outOfStockCount = perSkuKpis.Count(k => k.IsOutOfStock);

            // ── KPI 4: Average Daily Sales (system-wide) ──────
            // Tổng bán / Tổng số ngày có bán (union tất cả ngày)
            // Dùng HashSet để gộp tất cả ngày bán, không tính trùng
            var allSaleDates = new HashSet<DateTime>();
            var totalSold = 0;

            foreach (var agg in aggregates.Values)
            {
                totalSold += agg.TotalSold;
                foreach (var date in agg.SaleDates)
                    allSaleDates.Add(date);
            }

            var systemAvgDailySales = allSaleDates.Count > 0
                ? (decimal)totalSold / allSaleDates.Count
                : 0m;

            // ── KPI 5: Average Inventory Age (system-wide) ────
            // Trung bình InventoryAgeDays của tất cả SKU có hàng tồn
            var skusWithStock = perSkuKpis.Where(k => !k.IsOutOfStock).ToList();
            var avgInventoryAge = skusWithStock.Count > 0
                ? skusWithStock.Average(k => (double)k.InventoryAgeDays)
                : 0.0;

            return new SystemWideKpi
            {
                TotalSkus = totalSkus,
                TotalStockValue = Math.Round(totalStockValue, 2),
                OutOfStockCount = outOfStockCount,
                AvgDailySales = Math.Round(systemAvgDailySales, 2),
                AvgInventoryAgeDays = Math.Round(avgInventoryAge, 1)
            };
        }

        // the final method to generate a comprehensive KPI report combining both per-SKU and system-wide KPIs
        public KpiReport GenerateReport(
        IReadOnlyDictionary<string, ProductAggregate> aggregates,
        DateTime? referenceDate = null)
        {
            var perSkuKpis = CalculatePerSkuKpis(aggregates, referenceDate);
            var systemWideKpis = CalculateSystemWideKpis(perSkuKpis, aggregates);

            return new KpiReport
            {
                ReportId = Guid.NewGuid().ToString(),
                ExportedDate = DateTime.Now,
                Details = perSkuKpis,
                TotalStockValue = systemWideKpis.TotalStockValue,
                TotalProductsProcessed = systemWideKpis.TotalSkus,
                SystemWide = systemWideKpis
            };
        }
    }
}