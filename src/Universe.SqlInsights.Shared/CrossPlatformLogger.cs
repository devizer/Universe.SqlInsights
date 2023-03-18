using System;

namespace Universe.SqlInsights.Shared
{
    public interface ICrossPlatformLogger
    {
        public void Log(LogSeverity severity, string message, Exception exception);
    }

    public enum LogSeverity
    {
        Info,
        Warning,
        Error,
    }

    public static class CrossPlatformLoggerExtensions
    {
        public static void LogInformation(this ICrossPlatformLogger logger, string message)
        {
            logger?.Log(LogSeverity.Info, message, null);
        }

        public static void LogWarning(this ICrossPlatformLogger logger, Exception error, string message)
        {
            logger?.Log(LogSeverity.Warning, message, error);
        }
        public static void LogError(this ICrossPlatformLogger logger, Exception error, string message)
        {
            logger?.Log(LogSeverity.Error, message, error);
        }
    }

    public class NullCrossPlatformLogger : ICrossPlatformLogger
    {
        public static readonly ICrossPlatformLogger Instance = new NullCrossPlatformLogger();

        public void Log(LogSeverity severity, string message, Exception exception)
        {
        }
    }


}
