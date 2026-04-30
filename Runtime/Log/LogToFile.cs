using UnityEngine;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;

namespace CupkekGames.Diagnostics
{
    public class LogToFile : MonoBehaviour
    {
        public string Subfolder = "logs";
        public string Name = "logs";
        public string Extension = "txt";
        public int MaxLogs = 10;
        public bool IncludeStackTrace = true;
        public float FlushInterval = 2f; // Flush buffer every 2 seconds (configurable)
        public bool IncludeTimestamp = true;
        public int MaxBufferSize = 1000; // Prevent memory issues

        private string _currentLogPath;
        private readonly Queue<string> _logBuffer = new Queue<string>();

        void Awake()
        {
            string baseDir = Application.persistentDataPath;
            string dir = Path.Combine(baseDir, Subfolder);

            // Ensure the subfolder directory exists
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // If old Logs.txt exists, back it up with timestamp
            string oldLog = Path.Combine(dir, $"{Name}.{Extension}");
            if (File.Exists(oldLog))
            {
                try
                {
                    string firstLine = File.ReadLines(oldLog).FirstOrDefault();
                    string datePart;

                    if (firstLine != null && firstLine.StartsWith("Session start: "))
                    {
                        datePart = firstLine.Substring(15).Replace(':', '-').Replace(' ', '_');
                    }
                    else
                    {
                        Debug.LogWarning($"Could not parse session start from log file, using current time for backup");
                        datePart = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                    }

                    string backupName = $"{Name}_{datePart}.{Extension}";
                    File.Move(oldLog, Path.Combine(dir, backupName));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to backup old log file: {ex.Message}");
                }
            }

            // Create new Logs.txt with session header and system info
            _currentLogPath = Path.Combine(dir, $"{Name}.{Extension}");
            try
            {
                string systemInfo = GenerateSystemInfo();
                File.WriteAllText(_currentLogPath, $"Session start: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{systemInfo}\n");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create log file: {ex.Message}");
            }

            CleanupOldLogs(dir);
        }

        private string GenerateSystemInfo()
        {
            try
            {
                var systemInfo = new System.Text.StringBuilder();
                systemInfo.AppendLine("=== SYSTEM INFORMATION ===");

                // Screen resolution
                systemInfo.AppendLine(
                    $"Resolution: {Screen.width}x{Screen.height}@{Screen.currentResolution.refreshRateRatio.value}Hz");

                // Graphics API
                systemInfo.AppendLine($"Graphics API: {SystemInfo.graphicsDeviceType}");

                // GPU info
                systemInfo.AppendLine($"GPU: {SystemInfo.graphicsDeviceName}");

                // VRAM (approximate)
                int vramMB = SystemInfo.graphicsMemorySize;
                systemInfo.AppendLine($"VRAM: {(vramMB > 0 ? $"{vramMB} MB" : "Unknown")}");

                // CPU info
                systemInfo.AppendLine($"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");

                // OS info
                systemInfo.AppendLine($"OS: {SystemInfo.operatingSystem} ({SystemInfo.operatingSystemFamily})");

                // Additional useful info
                systemInfo.AppendLine($"Unity Version: {Application.unityVersion}");
                systemInfo.AppendLine($"Platform: {Application.platform}");
                systemInfo.AppendLine($"Device Model: {SystemInfo.deviceModel}");
                systemInfo.AppendLine($"Device Name: {SystemInfo.deviceName}");
                systemInfo.AppendLine($"Device Type: {SystemInfo.deviceType}");
                systemInfo.AppendLine($"System Memory: {SystemInfo.systemMemorySize} MB");

                systemInfo.AppendLine("==========================");
                return systemInfo.ToString();
            }
            catch (Exception ex)
            {
                return $"Failed to gather system info: {ex.Message}\n";
            }
        }

        void Start()
        {
            // Start the periodic flush coroutine
            StartCoroutine(PeriodicFlush());
        }

        private System.Collections.IEnumerator PeriodicFlush()
        {
            while (true)
            {
                yield return new WaitForSeconds(FlushInterval);

                if (_logBuffer.Count > 0)
                {
                    FlushBufferAsync();
                }
            }
        }

        void OnEnable() => Application.logMessageReceived += HandleLog;
        void OnDisable() => Application.logMessageReceived -= HandleLog;

        void OnApplicationQuit()
        {
            // Flush any remaining logs before Unity exits (use sync version for reliability)
            FlushBuffer();
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            // Check memory usage and force flush if needed
            if (_logBuffer.Count >= MaxBufferSize)
            {
                Debug.LogWarning("Log buffer is full, forcing immediate flush");
                FlushBufferAsync();
            }

            // Build log entry with timestamp
            string timestamp = IncludeTimestamp ? $"[{DateTime.Now:HH:mm:ss.fff}] " : "";
            string logEntry = $"{timestamp}[{type}] {logString}";

            // Include stack trace for errors and exceptions
            if (IncludeStackTrace && (type == LogType.Error || type == LogType.Exception))
            {
                logEntry += "\nStack Trace:\n" + stackTrace.Replace("\n", "\n");
            }

            // Add to buffer (Unity's log events are called on main thread)
            _logBuffer.Enqueue(logEntry);
        }

        private void FlushBuffer()
        {
            if (_logBuffer.Count == 0) return;

            try
            {
                using var writer = File.AppendText(_currentLogPath);
                while (_logBuffer.Count > 0)
                {
                    writer.WriteLine(_logBuffer.Dequeue());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write to log file: {ex.Message}");
            }
        }

        private async void FlushBufferAsync()
        {
            if (_logBuffer.Count == 0) return;

            var logsToWrite = new List<string>();
            while (_logBuffer.Count > 0)
            {
                logsToWrite.Add(_logBuffer.Dequeue());
            }

            try
            {
                await File.AppendAllLinesAsync(_currentLogPath, logsToWrite);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to write to log file: {ex.Message}");
            }
        }

        void CleanupOldLogs(string dir)
        {
            try
            {
                var files = Directory.GetFiles(dir, $"{Name}_*.{Extension}")
                    .OrderByDescending(File.GetCreationTime)
                    .ToList();

                foreach (var oldFile in files.Skip(MaxLogs))
                {
                    try
                    {
                        File.Delete(oldFile);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to delete old log file {oldFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to cleanup old logs: {ex.Message}");
            }
        }
    }
}
