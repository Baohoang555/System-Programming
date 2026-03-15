using InventoryKpiSystem.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InventoryKpiSystem.Infrastructure.Persistence
{
    public class FileTracker
    {
        private readonly string _registryFilePath;
        private readonly HashSet<string> _processedChecksums;
        private readonly object _lockObject = new object();
        private readonly ILogger? _logger;

        // Primary constructor
        public FileTracker(string registryDirectory = "data/processed-files", ILogger? logger = null)
        {
            _logger = logger;
            _registryFilePath = Path.Combine(registryDirectory, "processed-files-registry.json");

            var directory = Path.GetDirectoryName(_registryFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _processedChecksums = new HashSet<string>();
            LoadRegistry();
        }

        // Secondary constructor — delegates to primary so _processedChecksums is always initialized
        public FileTracker(string registryDirectory, Logging.ILogger infraLogger)
            : this(registryDirectory)
        {
            // infraLogger used externally; core ILogger stays null
        }

        public bool IsFileProcessed(string filePath)
        {
            var checksum = CalculateChecksum(filePath);
            lock (_lockObject)
                return _processedChecksums.Contains(checksum);
        }

        public void MarkAsProcessed(string filePath)
        {
            var checksum = CalculateChecksum(filePath);
            lock (_lockObject)
            {
                if (_processedChecksums.Add(checksum))
                {
                    _logger?.LogDebug($"Marked: {Path.GetFileName(filePath)} ({checksum[..8]}...)");
                    SaveRegistry();
                }
            }
        }

        public void UnmarkFile(string filePath)
        {
            var checksum = CalculateChecksum(filePath);
            lock (_lockObject)
            {
                if (_processedChecksums.Remove(checksum))
                    SaveRegistry();
            }
        }

        public int GetProcessedFileCount()
        {
            lock (_lockObject)
                return _processedChecksums.Count;
        }

        public void ClearRegistry()
        {
            lock (_lockObject)
            {
                _processedChecksums.Clear();
                SaveRegistry();
                _logger?.LogWarning("File registry CLEARED!");
            }
        }

        private string CalculateChecksum(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            var hash = sha256.ComputeHash(stream);
            var sb = new StringBuilder();
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private void LoadRegistry()
        {
            try
            {
                if (File.Exists(_registryFilePath))
                {
                    var json = File.ReadAllText(_registryFilePath);
                    var data = JsonSerializer.Deserialize<ProcessedFilesRegistry>(json);
                    if (data?.ProcessedChecksums != null)
                        foreach (var c in data.ProcessedChecksums)
                            _processedChecksums.Add(c);

                    _logger?.LogInfo($"Loaded {_processedChecksums.Count} processed file records");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Failed to load file registry, starting fresh", ex);
            }
        }

        private void SaveRegistry()
        {
            try
            {
                var data = new ProcessedFilesRegistry
                {
                    ProcessedChecksums = _processedChecksums.ToList(),
                    LastUpdated = DateTime.Now
                };
                File.WriteAllText(_registryFilePath,
                    JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                _logger?.LogError("Failed to save file registry", ex);
            }
        }

        private class ProcessedFilesRegistry
        {
            public List<string> ProcessedChecksums { get; set; } = new();
            public DateTime LastUpdated { get; set; }
        }
    }
}