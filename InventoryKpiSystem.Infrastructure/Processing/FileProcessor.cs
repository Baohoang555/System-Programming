using InventoryKpiSystem.Infrastructure.Queuing;

namespace InventoryKpiSystem.Infrastructure.Processing;

public class FileProcessor
{
    private readonly RetryHandler _retry;

    public FileProcessor(RetryHandler retry)
    {
        _retry = retry;
    }

    public ProcessingResult Process(FileTask task)
    {
        try
        {
            string content = File.ReadAllText(task.FilePath);

            Console.WriteLine($"[Processing] {task.FilePath}");

            if (content.Length < 5)
                throw new Exception("Invalid file");

            return new ProcessingResult(task.FilePath, true, "Success");
        }
        catch (Exception ex)
        {
            if (_retry.ShouldRetry(task.RetryCount))
            {
                task.RetryCount++;
                return new ProcessingResult(task.FilePath, false, "Retry needed");
            }

            return new ProcessingResult(task.FilePath, false, ex.Message);
        }
    }
}