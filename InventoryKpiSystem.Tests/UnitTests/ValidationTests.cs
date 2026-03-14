using InventoryKpiSystem.Core.Models;
using InventoryKpiSystem.Core.DataAccess;
using Xunit;

namespace InventoryKpiSystem.Tests.UnitTests
{
    public class ValidationTests
    {
        [Theory]
        [InlineData("", "P01", 10, 100)]    // Thiếu ID
        [InlineData("INV1", "P01", -5, 100)] // Số lượng âm 
        [InlineData("INV1", "P01", 10, -1)]  // Giá âm
        public void Invoice_InvalidData_ShouldReturnFalse(string id, string pId, int qty, decimal price)
        {
            var inv = new Invoice { InvoiceId = id, ProductId = pId, Quantity = qty, UnitPrice = price };
            Assert.False(DataValidator.IsValidInvoice(inv));
        }

        [Fact]
        public void PurchaseOrder_ValidData_ShouldReturnTrue()
        {
            var po = new PurchaseOrder { OrderId = "PO1", ProductId = "P01", Quantity = 100, UnitCost = 50 };
            Assert.True(DataValidator.IsValidPurchaseOrder(po));
        }
    }
}