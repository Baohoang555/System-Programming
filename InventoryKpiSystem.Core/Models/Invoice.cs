using System;
using System.Collections.Generic;

namespace InventoryKpiSystem.Core.Models
{
    public class Invoice
    {
        public string InvoiceID { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // ACCPAY = Nhập, ACCREC = Bán
        public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Total { get; set; }

        public List<LineItem> LineItems { get; set; } = new List<LineItem>();
    }

    public class LineItem
    {
        public string ItemCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitAmount { get; set; }
        public decimal LineAmount { get; set; }
        public decimal TaxAmount { get; set; }
    }
}