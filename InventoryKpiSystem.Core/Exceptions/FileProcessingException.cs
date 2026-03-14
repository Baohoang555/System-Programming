using System;

namespace InventoryKpiSystem.Core.Exceptions
{
    /// <summary>
    /// Ném ra khi có lỗi trong quá trình xử lý file (ví dụ: file không tồn tại, 
    /// không thể đọc file, hoặc lỗi định dạng file).
    /// </summary>
    public class FileProcessingException : Exception
    {
        public FileProcessingException() : base() { }

        public FileProcessingException(string message) : base(message) { }

        public FileProcessingException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}