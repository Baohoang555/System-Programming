using InventoryKpiSystem.Core.DataAccess;
using Xunit;
using Assert = Xunit.Assert; // resolve ambiguity with MSTest.Assert

namespace InventoryKpiSystem.Tests.UnitTests
{
    public class DataAccessTests
    {
        [Fact]
        public async Task Reader_InvoiceFileNotFound_ReturnsNull()
        {
            var reader = new JsonDataReader();
            var result = await reader.ReadInvoicesAsync("non_existent.json");
            Assert.Null(result);
        }

        [Fact]
        public async Task Reader_ProductFileNotFound_ReturnsNull()
        {
            var reader = new JsonDataReader();
            var result = await reader.ReadProductsAsync("non_existent.json");
            Assert.Null(result);
        }
    }
}