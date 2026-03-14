using System;

namespace InventoryKpiSystem.Core.Exceptions
{
    /// <summary>
    /// Ném ra khi dữ liệu đầu vào (ví dụ: từ JSON) không hợp lệ, thiếu trường bắt buộc, 
    /// sai định dạng hoặc vi phạm các quy tắc nghiệp vụ.
    /// </summary>
    public class DataValidationException : Exception
    {
        public DataValidationException() : base() { }

        public DataValidationException(string message) : base(message) { }

        public DataValidationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}