namespace CMCSPrototype.Services
{
    public class LoggingService : ILoggingService
    {
        private static readonly List<string> _logs = new();
        private static readonly object _lock = new();

        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public void LogError(string message, Exception? ex = null)
        {
            var fullMessage = ex != null ? $"{message} | Exception: {ex.Message}" : message;
            Log("ERROR", fullMessage);
        }

        public void LogWarning(string message)
        {
            Log("WARNING", message);
        }

        public List<string> GetRecentLogs(int count = 50)
        {
            lock (_lock)
            {
                return _logs.TakeLast(count).ToList();
            }
        }

        private void Log(string level, string message)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            
            lock (_lock)
            {
                _logs.Add(logEntry);
                if (_logs.Count > 1000) // Keep only last 1000 logs
                {
                    _logs.RemoveAt(0);
                }
            }

            // Also log to console
            Console.WriteLine(logEntry);
        }
    }
}
