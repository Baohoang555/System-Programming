namespace InventoryKpiSystem.Infrastructure.Processing;

public class RetryHandler
{
    private readonly int _maxRetries;

    public RetryHandler(int maxRetries)
    {
        _maxRetries = maxRetries;
    }

    public bool ShouldRetry(int retryCount)
    {
        return retryCount < _maxRetries;
    }
}