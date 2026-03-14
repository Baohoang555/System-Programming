using System.Threading.Tasks;

namespace InventoryKpiSystem.Core.Interfaces
{
    // TV4 implement interface này trong FileTracker.cs
    // Dùng để tránh xử lý trùng file (idempotency)
    public interface IFileTracker
    {
        // TV3 gọi TRƯỚC khi xử lý — check có bị duplicate không
        // fileName + checksum để đảm bảo cùng tên nhưng khác nội dung vẫn detect được
        Task<bool> IsProcessedAsync(string fileName, string checksum);

        // TV3 gọi SAU KHI xử lý thành công
        Task MarkAsProcessedAsync(string fileName, string checksum);

        // TV4 dùng nội bộ — load danh sách đã xử lý khi app khởi động
        Task LoadRegistryAsync();

        // TV4 dùng nội bộ — lưu registry xuống disk
        Task SaveRegistryAsync();
    }
}