using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FileAccessAgent
{
    /// <summary>
    /// Dual logging utility for PCAccess Agent.
    /// WHAT: Writes logs to Console (when interactive) and daily rolling log files in logs/ folder.
    /// REASON: Windows Services run headlessly in Session 0; file logs and Event Viewer provide diagnostics.
    /// </summary>
    public static class AgentLogger
    {
        private static readonly object _fileLock = new object();
        private static readonly string _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
        private const string EventSourceName = "PCAccessAgent";
        private const string EventLogName = "Application";

        static AgentLogger()
        {
            try
            {
                if (!Directory.Exists(_logDir))
                {
                    Directory.CreateDirectory(_logDir);
                }

                // Cleanup log files older than 14 days
                CleanOldLogs(14);
            }
            catch { }
        }

        public static void Info(string message = "")
        {
            Write(message, ConsoleColor.Gray, "INFO");
        }

        public static void Success(string message)
        {
            Write(message, ConsoleColor.Green, "SUCCESS");
        }

        public static void Warn(string message)
        {
            Write(message, ConsoleColor.Yellow, "WARN");
        }

        public static void Error(string message, Exception? ex = null)
        {
            string fullMsg = ex != null ? $"{message} | Exception: {ex.GetType().Name}: {ex.Message}" : message;
            Write(fullMsg, ConsoleColor.Red, "ERROR");

            // Write to Windows Event Log for fatal / critical errors
            try
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    if (!EventLog.SourceExists(EventSourceName))
                    {
                        EventLog.CreateEventSource(EventSourceName, EventLogName);
                    }
                    EventLog.WriteEntry(EventSourceName, fullMsg, EventLogEntryType.Error, 1001);
                }
            }
            catch { }
        }

        private static void Write(string message, ConsoleColor color, string level)
        {
            if (string.IsNullOrEmpty(message))
            {
                if (Environment.UserInteractive)
                {
                    try { Console.WriteLine(); } catch { }
                }
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string formattedConsole = $"[{DateTime.Now:HH:mm:ss}] {message}";
            string formattedFile = $"[{timestamp}] [{level}] {message}";

            // 1. Console Output if Interactive
            if (Environment.UserInteractive)
            {
                try
                {
                    Console.ForegroundColor = color;
                    Console.WriteLine(formattedConsole);
                    Console.ResetColor();
                }
                catch { }
            }

            // 2. Rolling File Log Output
            try
            {
                string logFile = Path.Combine(_logDir, $"agent_{DateTime.Now:yyyy-MM-dd}.log");
                lock (_fileLock)
                {
                    File.AppendAllText(logFile, formattedFile + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }

        private static void CleanOldLogs(int maxDays)
        {
            try
            {
                var files = Directory.GetFiles(_logDir, "agent_*.log");
                DateTime cutoff = DateTime.Now.AddDays(-maxDays);
                foreach (var file in files)
                {
                    var fi = new FileInfo(file);
                    if (fi.CreationTime < cutoff && fi.LastWriteTime < cutoff)
                    {
                        fi.Delete();
                    }
                }
            }
            catch { }
        }
    }
}
