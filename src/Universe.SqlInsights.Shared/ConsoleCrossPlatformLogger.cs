using System;

namespace Universe.SqlInsights.Shared;

public class ConsoleCrossPlatformLogger : ICrossPlatformLogger
{
    public static readonly ConsoleCrossPlatformLogger Instance = new ConsoleCrossPlatformLogger();

    public void Log(LogSeverity severity, string message, Exception exception)
    {
        var msg = $"{message}{(exception == null ? null : $"{Environment.NewLine}{exception}")}";
        try
        {
            ConsoleColor? fc = null;
            ConsoleColor? newColor = null;
            if (severity == LogSeverity.Warning)
                newColor = ConsoleColor.Yellow;
            if (severity == LogSeverity.Error)
                newColor = ConsoleColor.Red;

            if (newColor.HasValue)
            {
                fc = Console.ForegroundColor;
                Console.ForegroundColor = newColor.Value;
            }

            Console.WriteLine(msg);

            if (newColor.HasValue)
            {
                Console.ForegroundColor = fc.Value;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(msg);
        }

    }
}