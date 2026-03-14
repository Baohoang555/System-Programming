namespace InventoryKpiSystem.Infrastructure.Monitoring;

public class FileEventArgs
{
    public string FilePath { get; }
    public DateTime Time { get; }

    public FileEventArgs(string filePath)
    {
        FilePath = filePath;
        Time = DateTime.Now;
    }
}