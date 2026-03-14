namespace InventoryKpiSystem.Infrastructure.Queuing;

public class ProcessingResult
{
    public string FilePath { get; }
    public bool Success { get; }
    public string Message { get; }

    public ProcessingResult(string filePath, bool success, string message)
    {
        FilePath = filePath;
        Success = success;
        Message = message;
    }
}