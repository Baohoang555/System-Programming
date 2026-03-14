using System;
using System.IO;
using InventoryKpiSystem.Infrastructure.Logging;

class TestLogger
{
    static void Main()
    {
        Console.WriteLine("=== TESTING LOGGER.CS ===\n");

        // Test 1: Basic logging
        Console.WriteLine("Test 1: Basic Logging");
        var logger = new Logger("test_logs", "test.log");

        logger.LogInfo("This is an INFO message");
        logger.LogWarning("This is a WARNING message");
        logger.LogError("This is an ERROR message");
        logger.LogDebug("This is a DEBUG message (may not show if minimumLevel=Info)");
        logger.LogCritical("This is a CRITICAL message");

        Console.WriteLine("✓ Check console: Should see colored messages");
        Console.WriteLine("✓ Check file: test_logs/test.log should exist\n");

        // Test 2: Exception logging
        Console.WriteLine("Test 2: Exception Logging");
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            logger.LogError("Caught an exception", ex);
        }

        Console.WriteLine("✓ Exception should be logged with stack trace\n");

        // Test 3: Thread safety
        Console.WriteLine("Test 3: Thread Safety (10 threads x 100 logs)");
        var tasks = new System.Threading.Tasks.Task[10];
        for (int i = 0; i < 10; i++)
        {
            var threadId = i;
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    logger.LogInfo($"Thread {threadId} - Message {j}");
                }
            });
        }

        System.Threading.Tasks.Task.WaitAll(tasks);
        Console.WriteLine("✓ All threads completed - check log file for mixed output\n");

        // Test 4: Log rotation
        Console.WriteLine("Test 4: Log Rotation");
        logger.RotateLogFile();
        Console.WriteLine("✓ Check test_logs/archive/ folder for rotated log\n");

        Console.WriteLine("=== LOGGER TESTS COMPLETED ===");
        Console.WriteLine("\nVERIFY:");
        Console.WriteLine("1. Console shows colored messages");
        Console.WriteLine("2. File test_logs/test.log exists and has content");
        Console.WriteLine("3. File test_logs/archive/ has rotated log");
        Console.WriteLine("4. No errors or crashes");

        Console.ReadKey();
    }
}