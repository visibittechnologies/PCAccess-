using System;
using System.IO;

namespace UserLoginAgent
{
    /// <summary>
    /// Formatted console and file logger for UserLoginAgent.
    /// WHAT: Outputs structured colored logs and writes to a local log file.
    /// REASON: Provides clear operational visibility to the end user and troubleshooting logs.
    /// </summary>
    public static class UserAgentLogger
    {
        private static readonly object _lock = new object();
        private static string _logFilePath = "";

        static UserAgentLogger()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PCAccess", "UserAgentLogs");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                _logFilePath = Path.Combine(dir, $"user_agent_{DateTime.Now:yyyyMMdd}.log");
            }
            catch { }
        }

        public static void Info(string message)
        {
            WriteLog(ConsoleColor.Cyan, "[INFO]", message);
        }

        public static void Success(string message)
        {
            WriteLog(ConsoleColor.Green, "[SUCCESS]", message);
        }

        public static void Warn(string message)
        {
            WriteLog(ConsoleColor.Yellow, "[WARN]", message);
        }

        public static void Error(string message)
        {
            WriteLog(ConsoleColor.Red, "[ERROR]", message);
        }

        public static void Drive(string message)
        {
            WriteLog(ConsoleColor.Magenta, "[REMOTE DRIVE]", message);
        }

        private static void WriteLog(ConsoleColor color, string tag, string message)
        {
            lock (_lock)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[{timestamp}] ");
                Console.ForegroundColor = color;
                Console.Write($"{tag,-15} ");
                Console.ResetColor();
                Console.WriteLine(message);

                try
                {
                    if (!string.IsNullOrWhiteSpace(_logFilePath))
                    {
                        File.AppendAllText(_logFilePath, $"[{timestamp}] {tag} {message}{Environment.NewLine}");
                    }
                }
                catch { }
            }
        }
    }
}
