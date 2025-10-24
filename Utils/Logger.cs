using System;
using System.IO;
using System.Text;

namespace RevitClaudeMCP.Utils
{
    /// <summary>
    /// Simple logging utility
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string _logFilePath;
        private static StreamWriter _logWriter;

        public static event EventHandler<LogEventArgs> OnLog;
        public static event EventHandler<LogEventArgs> OnError;
        public static event EventHandler<LogEventArgs> OnWarning;

        /// <summary>
        /// Initialize logger
        /// </summary>
        public static void Initialize()
        {
            try
            {
                string logFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RevitClaudeMCP",
                    "Logs"
                );

                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _logFilePath = Path.Combine(logFolder, $"RevitClaudeMCP_{timestamp}.log");

                _logWriter = new StreamWriter(_logFilePath, true, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                Log($"Logger initialized: {_logFilePath}");
            }
            catch (Exception ex)
            {
                // Fallback: log to console if file logging fails
                Console.WriteLine($"Failed to initialize logger: {ex.Message}");
            }
        }

        /// <summary>
        /// Log info message
        /// </summary>
        public static void Log(string message)
        {
            WriteLog("INFO", message);
            OnLog?.Invoke(null, new LogEventArgs { Level = "INFO", Message = message });
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public static void LogError(string message, Exception ex = null)
        {
            string fullMessage = ex != null ? $"{message}\n{ex}" : message;
            WriteLog("ERROR", fullMessage);
            OnError?.Invoke(null, new LogEventArgs { Level = "ERROR", Message = fullMessage, Exception = ex });
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public static void LogWarning(string message)
        {
            WriteLog("WARNING", message);
            OnWarning?.Invoke(null, new LogEventArgs { Level = "WARNING", Message = message });
        }

        /// <summary>
        /// Write log entry
        /// </summary>
        private static void WriteLog(string level, string message)
        {
            lock (_lock)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] [{level}] {message}";

                    // Write to file
                    _logWriter?.WriteLine(logEntry);

                    // Also write to debug output
                    System.Diagnostics.Debug.WriteLine(logEntry);
                }
                catch (Exception ex)
                {
                    // Fallback: write to console if logging fails
                    Console.WriteLine($"Logging failed: {ex.Message}");
                    Console.WriteLine(message);
                }
            }
        }

        /// <summary>
        /// Close logger
        /// </summary>
        public static void Close()
        {
            lock (_lock)
            {
                try
                {
                    Log("Logger closing...");
                    _logWriter?.Flush();
                    _logWriter?.Close();
                    _logWriter?.Dispose();
                    _logWriter = null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error closing logger: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get log file path
        /// </summary>
        public static string GetLogFilePath()
        {
            return _logFilePath;
        }
    }

    /// <summary>
    /// Log event arguments
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        public string Level { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
