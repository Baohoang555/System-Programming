using System;

namespace InventoryKpiSystem.Core.Interfaces
{
    // TV4 implement interface này trong Logger.cs
    // Tách ILogger riêng để sau này có thể swap
    // từ Console logger → File logger → Cloud logger
    // mà không đụng vào code của TV khác
    public interface ILogger
    {
        // Thông tin thông thường — file đến, xử lý xong
        void LogInfo(string message);

        // Cảnh báo — dữ liệu thiếu field nhưng vẫn xử lý được
        void LogWarning(string message);

        // Lỗi nghiêm trọng — file corrupt, parse thất bại
        void LogError(string message, Exception? exception = null);

        // Debug — chỉ hiện khi development
        void LogDebug(string message);
    }
}