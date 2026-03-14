using System;

namespace InventoryKpiSystem.Core.Models
{
    public class ProductAggregate
    {
        // PurchaseOrder 
        public int TotalPurchased { get; set; } = 0;
        public decimal TotalPurchaseCost { get; set; } = 0m;

        // Invoice 
        public int TotalSold { get; set; } = 0;

        // use MaxValue because the value  < MaxValue -> first order/invoice all way update
        public DateTime EarliestPurchaseDate { get; set; } = DateTime.MaxValue;
        public DateTime LatestPurchaseDate { get; set; } = DateTime.MinValue;

        public DateTime EarliestSaleDate { get; set; } = DateTime.MaxValue;
        public DateTime LatestSaleDate { get; set; } = DateTime.MinValue;

        // Dung Hashset để tránh tính toán lại nếu đã có dữ liệu vi có thể có nhiều file cùng sản phẩm nhưng chỉ cần tính 1 lần.
        public HashSet<DateTime> SaleDates { get; set; } = new();
    }
}