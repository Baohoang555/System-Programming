using System.Collections.Concurrent;

namespace InventoryKpiSystem.Infrastructure.Queuing;

public class FileProcessingQueue
{
    private readonly ConcurrentQueue<FileTask> _queue = new();

    public void Enqueue(FileTask task)
    {
        _queue.Enqueue(task);
    }

    public bool TryDequeue(out FileTask? task)
    {
        return _queue.TryDequeue(out task);
    }
}