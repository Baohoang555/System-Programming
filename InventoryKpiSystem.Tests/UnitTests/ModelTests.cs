using InventoryKPI.Models;
using InventoryKPI.Data;
using Xunit;

namespace InventoryKPI.Tests
{
    public class DataValidatorTests
    {
        // --- KIỂM THỬ HÓA ĐƠN BÁN HÀNG (INVOICE) ---

        [Fact]
        public void IsValidInvoice_WithValidData_ShouldReturnTrue()
        {
            // Kiểm tra trường hợp dữ liệu đầy đủ và chính xác
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
        [InlineData("", "P1", 10, 100)]    // Lỗi: Thiếu ID hóa đơn
        [InlineData("INV1", "", 10, 100)]  // Lỗi: Thiếu ID sản phẩm
        [InlineData("INV1", "P1", 0, 100)]  // Lỗi: Số lượng không thể bằng 0
        [InlineData("INV1", "P1", 10, -50)] // Lỗi: Đơn giá không được âm
        public void IsValidInvoice_WithInvalidData_ShouldReturnFalse(string id, string pId, int qty, decimal price)
        {
            // Kiểm tra các trường hợp biên và dữ liệu sai logic
            var invoice = new Invoice
            {
                InvoiceId = id,
                ProductId = pId,
                Quantity = qty,
                UnitPrice = price
            };
            Assert.False(DataValidator.IsValidInvoice(invoice));
        }

        // --- KIỂM THỬ ĐƠN NHẬP HÀNG (PURCHASE ORDER) ---

        [Fact]
        public void IsValidPurchaseOrder_WithValidData_ShouldReturnTrue()
        {
            // Kiểm tra đơn nhập hàng hợp lệ
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
            // Kiểm tra lỗi logic khi giá vốn nhập vào bị âm
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