using System;

namespace Universe.SqlInsights.Shared;

public class NullCrossPlatformLogger : ICrossPlatformLogger
{
    public static readonly ICrossPlatformLogger Instance = new NullCrossPlatformLogger();

    public void Log(LogSeverity severity, string message, Exception exception)
    {
    }
}