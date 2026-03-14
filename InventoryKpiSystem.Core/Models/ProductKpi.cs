namespace InventoryKpiSystem.Core.Models
{
    public class ProductKpi
    {
        public string ProductId { get; set; } = string.Empty;
        public int InventoryAgeDays { get; set; }
        public decimal StockValue { get; set; } // // KPI 2
        public int CurrentStock { get; set; } // KPI 3 input
        public bool IsOutOfStock { get; set; } // KPI 3
        public decimal AvgDailySales { get; set; } // KPI 4

    }
}