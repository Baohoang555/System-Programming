using System;
using System.Collections.Generic;


namespace InventoryKpiSystem.Core.Models
{
    public class KpiReport
    {
        public string ReportId { get; set; } = string.Empty;
        public DateTime ExportedDate { get; set; }

        // Danh sách kết quả KPI cho từng sản phẩm
        public List<ProductKpi> Details { get; set; } = new List<ProductKpi>();

        // Tóm tắt chung (Ví dụ: Tổng giá trị kho)
        public decimal TotalStockValue { get; set; }
        public int TotalProductsProcessed { get; set; }

        public SystemWideKpi SystemWide { get; set; } = new();
        
    }
}