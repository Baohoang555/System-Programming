namespace InventoryKpiSystem.Core.Models
{
    public class ProductKpi
    {
        public string ProductId { get; set; } = string.Empty;
        public decimal InventoryTurnover { get; set; }
        public decimal StockValue { get; set; }
    }
}