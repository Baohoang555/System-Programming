using InventoryKpiSystem.Core.Models;

namespace InventoryKpiSystem.Core.DataAccess
{
    public static class DataValidator
    {
        public static bool IsValidInvoice(Invoice inv) =>
            !string.IsNullOrEmpty(inv.InvoiceId) && inv.Quantity > 0 && inv.UnitPrice >= 0;

        public static bool IsValidPurchaseOrder(PurchaseOrder po) =>
            !string.IsNullOrEmpty(po.OrderId) && po.Quantity > 0 && po.UnitCost >= 0;
    }
}