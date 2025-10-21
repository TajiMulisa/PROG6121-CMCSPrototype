namespace CMCSPrototype.Services
{
    public interface ILoggingService
    {
        void LogInfo(string message);
        void LogError(string message, Exception? ex = null);
        void LogWarning(string message);
        List<string> GetRecentLogs(int count = 50);
    }
}
