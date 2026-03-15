using InventoryKpiSystem.Core.Models;

namespace InventoryKpiSystem.Core.DataAccess
{
    public static class DataValidator
    {
        public static bool IsValidInvoice(Invoice inv) =>
            !string.IsNullOrEmpty(inv.InvoiceId) &&
            !string.IsNullOrEmpty(inv.ProductId) &&  
            inv.Quantity > 0 &&
            inv.UnitPrice >= 0;

        public static bool IsValidPurchaseOrder(PurchaseOrder po) =>
            !string.IsNullOrEmpty(po.OrderId) &&
            !string.IsNullOrEmpty(po.ProductId) &&   
            po.Quantity > 0 &&
            po.UnitCost >= 0;
    }
}