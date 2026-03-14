// Interfaces/IDataReader.cs
using InventoryKpiSystem.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryKpiSystem.Core.Interfaces
{
    // TV1 implement interface này trong JsonDataReader.cs
    // Generic T cho phép đọc cả PurchaseOrder lẫn Invoice
    // mà không cần 2 interface riêng
    public interface IDataReader<T>
    {
        // Đọc và deserialize JSON file → List<T>
        // Async vì file I/O không nên block thread
        Task<List<T>> ReadAsync(string filePath);

        // Đọc nhiều files cùng lúc — dùng cho batch processing
        Task<List<T>> ReadManyAsync(IEnumerable<string> filePaths);
    }
}