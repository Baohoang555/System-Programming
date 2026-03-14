namespace InventoryKpiSystem.Core.Models
{
    public class Invoice
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime Date { get; set; }
    }
}