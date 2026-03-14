using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InventoryKpiSystem.Infrastructure.Persistence
{
    /// <summary>
    /// Tracks processed files để tránh duplicate processing.
    /// Uses SHA256 checksum để identify files based on content.
    /// </summary>
    public class FileTracker
    {
        private readonly string _registryFilePath;
        private readonly HashSet<string> _processedChecksums;
        private readonly object _lockObject = new object();
        private readonly ILogger? _logger;

        public FileTracker(string registryDirectory = "data/processed-files", ILogger? logger = null)
        {
            _logger = logger;
            _registryFilePath = Path.Combine(registryDirectory, "processed-files-registry.json");

            // Tạo directory nếu chưa có 
            var directory = Path.GetDirectoryName(_registryFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _processedChecksums = new HashSet<string>();
            LoadRegistry();
        }

        #region Public Methods

        /// <summary>
        /// Kiểm tra file đã được xử lý chưa
        /// </summary>
        public bool IsFileProcessed(string filePath)
        {
            var checksum = CalculateChecksum(filePath);
            lock (_lockObject)
            {
                return _processedChecksums.Contains(checksum);
            }
        }

        /// <summary>
        /// Đánh dấu file đã được xử lý
        /// </summary>
        public void MarkAsProcessed(string filePath)
        {
            var checksum = CalculateChecksum(filePath);
            lock (_lockObject)
            {
                if (_processedChecksums.Add(checksum))
                {
                    _logger?.LogDebug($"Marked as processed: {Path.GetFileName(filePath)} ({checksum.Substring(0, 8)}...)");
                    SaveRegistry();
                }
            }
        }

        /// <summary>
        /// Xóa file khỏi registry (để reprocess)
        /// </summary>
        public void UnmarkFile(string filePath)
        {
            var checksum = CalculateChecksum(filePath);
            lock (_lockObject)
            {
                if (_processedChecksums.Remove(checksum))
                {
                    _logger?.LogDebug($"Unmarked file: {Path.GetFileName(filePath)}");
                    SaveRegistry();
                }
            }
        }

        /// <summary>
        /// Đếm số file đã xử lý
        /// </summary>
        public int GetProcessedFileCount()
        {
            lock (_lockObject)
            {
                return _processedChecksums.Count;
            }
        }

        /// <summary>
        /// Reset registry (NGUY HIỂM!)
        /// </summary>
        public void ClearRegistry()
        {
            lock (_lockObject)
            {
                _processedChecksums.Clear();
                SaveRegistry();
                _logger?.LogWarning("File registry CLEARED!");
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Tính SHA256 checksum của file
        /// </summary>
        private string CalculateChecksum(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hashBytes = sha256.ComputeHash(stream);

            var sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Load registry từ JSON file
        /// </summary>
        private void LoadRegistry()
        {
            try
            {
                if (File.Exists(_registryFilePath))
                {
                    var json = File.ReadAllText(_registryFilePath);
                    var data = JsonSerializer.Deserialize<ProcessedFilesRegistry>(json);

                    if (data?.ProcessedChecksums != null)
                    {
                        foreach (var checksum in data.ProcessedChecksums)
                        {
                            _processedChecksums.Add(checksum);
                        }
                        _logger?.LogInfo($"Loaded {_processedChecksums.Count} processed file records");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Failed to load file registry, starting fresh", ex);
            }
        }

        /// <summary>
        /// Save registry vào JSON file
        /// </summary>
        private void SaveRegistry()
        {
            try
            {
                var data = new ProcessedFilesRegistry
                {
                    ProcessedChecksums = _processedChecksums.ToList(),
                    LastUpdated = DateTime.Now
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_registryFilePath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError("Failed to save file registry", ex);
            }
        }

        #endregion

        /// <summary>
        /// Model cho registry file
        /// </summary>
        private class ProcessedFilesRegistry
        {
            public List<string> ProcessedChecksums { get; set; } = new();
            public DateTime LastUpdated { get; set; }
        }
    }
}