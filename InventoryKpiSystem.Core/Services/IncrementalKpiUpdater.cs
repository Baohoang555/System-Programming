using InventoryKpiSystem.Core.Exceptions;
using InventoryKpiSystem.Core.Interfaces;
using InventoryKpiSystem.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace InventoryKpiSystem.Core.Services
{
    public class IncrementalKpiUpdater
    {
        // this concurrent dictionary is used to create 1 aggregate per product, and update it incrementally when processing each file.
        private readonly ConcurrentDictionary<string, ProductAggregate> _aggregates = new ConcurrentDictionary<string, ProductAggregate>();

        // This Method processes a batch of new purchase orders, updating the in-memory aggregates for each product.
        public void ProcessNewPurchaseOrders(List<PurchaseOrder> newOrders)
        {
            foreach (var order in newOrders)
            {
                // Lấy aggregate cũ hoặc tạo mới nếu chưa có
                var aggregate = _aggregates.GetOrAdd(
                    order.ProductId,
                    _ => new ProductAggregate()
                );

                // Lock chính aggregate đó — không lock toàn bộ dictionary
                // → các ProductId khác nhau vẫn chạy song song
                lock (aggregate)
                {
                    // Cộng dồn số lượng và chi phí mua
                    aggregate.TotalPurchased += order.Quantity;
                    aggregate.TotalPurchaseCost += order.Quantity * order.UnitCost;

                    if (order.Date < aggregate.EarliestPurchaseDate)
                        aggregate.EarliestPurchaseDate = order.Date;

                    if (order.Date > aggregate.LatestPurchaseDate)
                        aggregate.LatestPurchaseDate = order.Date;
                }
            }
        }
        // This Methoid 2 processes a batch of new invoices, updating the in-memory aggregates for each product.
        public void ProcessNewInvoices(List<Invoice> newInvoices)
        {
            foreach (var invoice in newInvoices)
            {
                var aggregate = _aggregates.GetOrAdd(
                    invoice.ProductId,
                    _ => new ProductAggregate()
                );

                lock (aggregate)
                {
                    // Cộng dồn số lượng bán
                    aggregate.TotalSold += invoice.Quantity;

                    // Cập nhật ngày bán sớm nhất / muộn nhất
                    if (invoice.Date < aggregate.EarliestSaleDate)
                        aggregate.EarliestSaleDate = invoice.Date;

                    if (invoice.Date > aggregate.LatestSaleDate)
                        aggregate.LatestSaleDate = invoice.Date;

                    // HashSet tự loại trùng
                    // → mỗi ngày chỉ đếm 1 lần dù có 100 invoice cùng ngày
                    // → dùng để tính số ngày thực sự có bán hàng
                    aggregate.SaleDates.Add(invoice.Date.Date);
                }
            }
        }

        public IReadOnlyDictionary<string, ProductAggregate> GetAllAggregates()
        {
            return _aggregates;
        }

        public ProductAggregate? GetAggregate(string productId)
        {
            _aggregates.TryGetValue(productId, out var aggregate);
            return aggregate;
        }

        public void RestoreFromSnapshot(Dictionary<string, ProductAggregate> snapshot)
        {
            foreach (var kvp in snapshot)
            {
                _aggregates[kvp.Key] = kvp.Value;
            }
        }
    }
}