using System.IO;

namespace InventoryKpiSystem.Infrastructure.Monitoring;

public class InventoryWatcher
{
    private readonly FileSystemWatcher _watcher;

    public event Action<FileEventArgs>? OnFileDetected;

    public InventoryWatcher(string folderPath)
    {
        _watcher = new FileSystemWatcher(folderPath, "*.json");

        _watcher.Created += OnCreated;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnCreated(object sender, FileSystemEventArgs e)
    {
        OnFileDetected?.Invoke(new FileEventArgs(e.FullPath));
    }
}