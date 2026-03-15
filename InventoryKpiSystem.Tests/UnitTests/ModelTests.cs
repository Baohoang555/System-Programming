using InventoryKpiSystem.Core.Models;
using InventoryKpiSystem.Core.DataAccess;
using Xunit;
using Assert = Xunit.Assert;

namespace InventoryKpiSystem.Tests.UnitTests
{
    public class ModelTests
    {
        // --- INVOICE VALIDATION ---

        [Fact]
        public void IsValidInvoice_WithValidData_ShouldReturnTrue()
        {
            var invoice = new Invoice
            {
                InvoiceId = "INV-001",
                ProductId = "PROD-A",
                Quantity = 10,
                UnitPrice = 150.0m
            };
            Assert.True(DataValidator.IsValidInvoice(invoice));
        }

        [Theory]
        [InlineData("", "P1", 10, 100)]    // Missing InvoiceId
        [InlineData("INV1", "", 10, 100)]  // Missing ProductId
        [InlineData("INV1", "P1", 0, 100)] // Zero quantity
        [InlineData("INV1", "P1", 10, -50)]// Negative price
        public void IsValidInvoice_WithInvalidData_ShouldReturnFalse(
            string id, string pId, int qty, decimal price)
        {
            var invoice = new Invoice
            {
                InvoiceId = id,
                ProductId = pId,
                Quantity = qty,
                UnitPrice = price
            };
            Assert.False(DataValidator.IsValidInvoice(invoice));
        }

        // --- PURCHASE ORDER VALIDATION ---

        [Fact]
        public void IsValidPurchaseOrder_WithValidData_ShouldReturnTrue()
        {
            var po = new PurchaseOrder
            {
                OrderId = "PO-999",
                ProductId = "PROD-B",
                Quantity = 100,
                UnitCost = 85.5m
            };
            Assert.True(DataValidator.IsValidPurchaseOrder(po));
        }

        [Fact]
        public void IsValidPurchaseOrder_WithNegativeCost_ShouldReturnFalse()
        {
            var po = new PurchaseOrder
            {
                OrderId = "PO-999",
                ProductId = "PROD-B",
                Quantity = 100,
                UnitCost = -10.0m
            };
            Assert.False(DataValidator.IsValidPurchaseOrder(po));
        }
    }
}