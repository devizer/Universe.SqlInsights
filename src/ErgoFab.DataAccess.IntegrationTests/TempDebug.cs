using System.Diagnostics;
using System.Reflection;
using ErgoFab.DataAccess.IntegrationTests.Library;

internal partial class TempDebug
{
    static readonly DateTime StartAt = DateTime.Now;
    private static Lazy<string> _Assembly = new Lazy<string>(() => { return Path.GetFileName(Assembly.GetExecutingAssembly().Location); });

    public static void WriteLine(string message)
    {
        var debugLogFolder = Environment.GetEnvironmentVariable("TESTS_DEBUG_FOLDER");
        if (string.IsNullOrEmpty(debugLogFolder)) return;
        var debugLogFile = Path.Combine(debugLogFolder, _Assembly.Value + " " + StartAt.ToString("yyyy-MM-dd HH꞉mm꞉ss") + ".log");
        TryAndForget.Execute(() => Directory.CreateDirectory(Path.GetDirectoryName(debugLogFile)));

        try
        {
            var line = string.IsNullOrEmpty(message) ? message : $"{DateTime.Now} {message}";
            File.AppendAllLines(debugLogFile, new[] { line,  });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"{DateTime.Now} {message}");
        }

    }
}