using System;
using System.Collections.Generic;

namespace InventoryKpiSystem.Core.Models
{
    public class KpiReport
    {
        public string ReportId { get; set; } = string.Empty;
        public DateTime ExportedDate { get; set; }
        public List<ProductKpi> Details { get; set; } = new List<ProductKpi>();
        public decimal TotalStockValue { get; set; }
    }
}