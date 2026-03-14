using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryKpiSystem.Core.Interfaces
{
    // TV3 implement interface này trong FileProcessor.cs
    // TV2 định nghĩa contract để các TV biết FileProcessor làm gì
    public interface IFileProcessor
    {
        // Xử lý 1 file cụ thể — async vì đọc file I/O
        // Trả về true nếu xử lý thành công, false nếu thất bại
        Task<bool> ProcessFileAsync(string filePath);

        // Xử lý toàn bộ 1 batch files cùng lúc
        // Được gọi sau khi InventoryWatcher gom đủ files trong giờ
        Task ProcessBatchAsync(IEnumerable<string> filePaths);
    }
}