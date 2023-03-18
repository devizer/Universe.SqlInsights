using System;
using Microsoft.Extensions.Logging;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.NetCore
{
    public class NetCoreLogger : ICrossPlatformLogger
    {
        private ILogger Logger;

        public NetCoreLogger(ILogger logger)
        {
            Logger = logger;
        }

        public void Log(LogSeverity severity, string message, Exception exception)
        {
            if (severity == LogSeverity.Info)
                Logger?.LogInformation(message);
#if !NETCOREAPP1_1 && !NETCOREAPP1_0            
            if (severity == LogSeverity.Warning)
                Logger?.LogWarning(exception, message);
            if (severity == LogSeverity.Error)
                Logger?.LogError(exception, message);
#else
            if (severity == LogSeverity.Warning)
                Logger?.LogWarning(message + (exception == null ? null : $"{Environment.NewLine}{exception}"));
            if (severity == LogSeverity.Error)
                Logger?.LogError(message+ (exception == null ? null : $"{Environment.NewLine}{exception}"));
#endif
        }
    }
}
