namespace InventoryKpiSystem.Core.Models
{
    public class SystemWideKpi
    {
        public int TotalSkus { get; set; }  // KPI 1
        public decimal TotalStockValue { get; set; }  // KPI 2
        public int OutOfStockCount { get; set; }  // KPI 3
        public decimal AvgDailySales { get; set; }  // KPI 4
        public double AvgInventoryAgeDays { get; set; }  // KPI 5
    }
}