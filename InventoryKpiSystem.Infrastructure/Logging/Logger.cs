using System;
using System.IO;

namespace InventoryKpiSystem.Infrastructure.Logging
{
   
    public class Logger : ILogger
    {
        private readonly string _logDirectory;
        private readonly string _logFileName;
        private readonly object _lockObject = new object();
        private readonly bool _enableConsoleOutput;
        private readonly bool _enableFileOutput;
        private readonly LogLevel _minimumLevel;

        public Logger(
            string logDirectory = "logs",
            string logFileName = "app.log",
            bool enableConsoleOutput = true,
            bool enableFileOutput = true,
            LogLevel minimumLevel = LogLevel.Info)
        {
            _logDirectory = logDirectory;
            _logFileName = logFileName;
            _enableConsoleOutput = enableConsoleOutput;
            _enableFileOutput = enableFileOutput;
            _minimumLevel = minimumLevel;

            if (_enableFileOutput && !Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        public void Log(LogLevel level, string message, Exception? exception = null)
        {
            if (level < _minimumLevel) return;

            lock (_lockObject)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = FormatLogEntry(timestamp, level, message, exception);

                if (_enableConsoleOutput)
                    WriteToConsole(level, logEntry);

                if (_enableFileOutput)
                    WriteToFile(logEntry);
            }
        }

        public void LogInfo(string message) => Log(LogLevel.Info, message);
        public void LogWarning(string message) => Log(LogLevel.Warning, message);
        public void LogError(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);
        public void LogDebug(string message) => Log(LogLevel.Debug, message);
        public void LogCritical(string message, Exception? exception = null) => Log(LogLevel.Critical, message, exception);

        private string FormatLogEntry(string timestamp, LogLevel level, string message, Exception? exception)
        {
            var levelString = level.ToString().ToUpper().PadRight(8);
            var logLine = $"[{timestamp}] [{levelString}] {message}";

            if (exception != null)
            {
                logLine += $"\n   Exception: {exception.GetType().Name}: {exception.Message}";
                logLine += $"\n   StackTrace: {exception.StackTrace}";

                if (exception.InnerException != null)
                {
                    logLine += $"\n   Inner: {exception.InnerException.Message}";
                }
            }

            return logLine;
        }

        private void WriteToConsole(LogLevel level, string logEntry)
        {
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };

            Console.WriteLine(logEntry);
            Console.ForegroundColor = originalColor;
        }

        private void WriteToFile(string logEntry)
        {
            try
            {
                var logFilePath = Path.Combine(_logDirectory, _logFileName);
                File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CRITICAL ERROR: Failed to write log to file: {ex.Message}");
            }
        }


        public void RotateLogFile()
        {
            lock (_lockObject)
            {
                try
                {
                    var logFilePath = Path.Combine(_logDirectory, _logFileName);
                    if (File.Exists(logFilePath))
                    {
                        var archiveName = $"{Path.GetFileNameWithoutExtension(_logFileName)}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(_logFileName)}";
                        var archiveDir = Path.Combine(_logDirectory, "archive");

                        Directory.CreateDirectory(archiveDir);
                        File.Move(logFilePath, Path.Combine(archiveDir, archiveName));

                        LogInfo($"Log rotated: {archiveName}");
                    }
                }
                catch (Exception ex)
                {
                    LogError("Failed to rotate log file", ex);
                }
            }
        }
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    public interface ILogger
    {
        void Log(LogLevel level, string message, Exception? exception = null);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? exception = null);
        void LogDebug(string message);
        void LogCritical(string message, Exception? exception = null);
    }
}