namespace InventoryKpiSystem.Core.Models
{
    public class PurchaseOrder
    {
        public string OrderId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime Date { get; set; }
    }
}