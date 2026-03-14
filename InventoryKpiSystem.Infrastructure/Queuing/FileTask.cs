namespace InventoryKpiSystem.Infrastructure.Queuing;

public class FileTask
{
    public string FilePath { get; }
    public int RetryCount { get; set; }

    public FileTask(string path)
    {
        FilePath = path;
    }
}