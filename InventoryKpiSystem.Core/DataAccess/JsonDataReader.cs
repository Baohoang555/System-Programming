using System.Text.Json;
using InventoryKpiSystem.Core.Models;

namespace InventoryKpiSystem.Core.DataAccess
{
    public class InvoiceDataWrapper
    {
        public List<Invoice> Invoices { get; set; } = new();
    }

    public class ProductDataWrapper
    {
        public List<Product> Items { get; set; } = new();
    }

    public class JsonDataReader
    {
        public async Task<List<Invoice>?> ReadInvoicesAsync(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var stream = File.OpenRead(filePath);
            var result = await JsonSerializer.DeserializeAsync<InvoiceDataWrapper>(stream, options);
            return result?.Invoices;
        }

        public async Task<List<Product>?> ReadProductsAsync(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var stream = File.OpenRead(filePath);
            var result = await JsonSerializer.DeserializeAsync<ProductDataWrapper>(stream, options);
            return result?.Items;
        }
    }
}