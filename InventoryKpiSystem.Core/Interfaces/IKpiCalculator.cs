using InventoryKpiSystem.Core.Models;
using System;
using System.Collections.Generic;

public interface IKpiCalculator
{
    // TV3 gọi cái này sau mỗi batch
    KpiReport GenerateReport(IReadOnlyDictionary<string, ProductAggregate> aggregates, DateTime? referenceDate = null);

    // TV4 có thể gọi riêng nếu chỉ cần per SKU
    List<ProductKpi> CalculatePerSkuKpis(
        IReadOnlyDictionary<string, ProductAggregate> aggregates,
        DateTime? referenceDate = null
    );

    // TV4 có thể gọi riêng nếu chỉ cần system-wide
    SystemWideKpi CalculateSystemWideKpis(
        List<ProductKpi> perSkuKpis,
        IReadOnlyDictionary<string, ProductAggregate> aggregates
    );
}