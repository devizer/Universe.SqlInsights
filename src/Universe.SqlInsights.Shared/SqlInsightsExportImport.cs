using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Universe.SqlInsights.Shared;

#if NETSTANDARD2_0_OR_GREATER || NET461

// TODO: For Import() sorting by 'Id Desc' is useless on Dashboard. Should be sorted by 'At Desc'
public class SqlInsightsExportImport 
{
    private readonly ISqlInsightsStorage Storage;
    public int BufferSize { get; set; } = 32768;
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Fastest;

    public SqlInsightsExportImport(ISqlInsightsStorage storage)
    {
        Storage = storage;
    }


    public async Task Export(Stream stream)
    {
        var bufferSize = Math.Max(1024, BufferSize);
        using var bufferedStream = new BufferedStream(stream, bufferSize);
        using ZipArchive zipArchive = new ZipArchive(bufferedStream, ZipArchiveMode.Create, false);
        string id = System.Environment.MachineName + ":" + Guid.NewGuid().ToString("N");
        var sessions = (await Storage.GetSessions()).ToList();
        var summaryJson = new { Id = id, Sessions = sessions };
        ZipArchiveEntry zipSummaryEntry = zipArchive.CreateEntry("Summary.json", CompressionLevel);
        using (var streamSummary = zipSummaryEntry.Open())
        {
            await System.Text.Json.JsonSerializer.SerializeAsync(streamSummary, summaryJson);
        }

        if (EnableDebugLog)
        {
            var p = Process.GetCurrentProcess();
            AppendLog($"Starting export. Memory: {p.WorkingSet64 / 1024:n0} Kb");
        }


        Stopwatch startAt = Stopwatch.StartNew();
        int totalActions = 0;
        foreach (var session in sessions)
        {
            ZipArchiveEntry zipSessionEntry = zipArchive.CreateEntry($"Session-{session.IdSession:f0}.json", CompressionLevel);
            using var streamSessionRaw = zipSessionEntry.Open();
            using var streamSession = new BufferedStream(streamSessionRaw, bufferSize);
            streamSession.Write(ArrayStart, 0, ArrayStart.Length);

            bool isNext = false;
            // Console.WriteLine($"[DEBUG ConnectionString] STARTING NON-BUFFERED ACTIONS QUERY");
            var actions = await Storage.GetActionsByKeyPath(session.IdSession, null, int.MaxValue - 1, null, null, null);
            foreach (ActionDetailsWithCounters action in actions)
            {
                if (isNext) streamSession.Write(NextSeparator, 0, NextSeparator.Length);
                streamSession.Write(Tab, 0, Tab.Length);

                // await System.Text.Json.JsonSerializer.SerializeAsync(streamSession, action);
                await System.Text.Json.JsonSerializer.SerializeAsync(streamSession, action);

                isNext = true;
                totalActions++;
                if (totalActions % 1000 == 0 && EnableDebugLog)
                {
                    var p = Process.GetCurrentProcess();
                    AppendLog($"Actions Progress: {totalActions}. Memory: {p.WorkingSet64 / 1024:n0} Kb");
                }
            }

            streamSession.Write(ArrayEnd, 0, ArrayEnd.Length);
        }

        if (EnableDebugLog)
        {
            var p = Process.GetCurrentProcess();
            AppendLog($"Total Actions: {totalActions}. Memory: {p.WorkingSet64 / 1024:n0} Kb");
        }
        AppendLog($"Duration: {startAt.Elapsed}");

        ZipArchiveEntry zipLogEntry = zipArchive.CreateEntry("Log.log", CompressionLevel);
        using (var streamLog = zipLogEntry.Open())
        using (StreamWriter wr = new StreamWriter(streamLog))
        {
            wr.Write(Log);
        }
    }
    private async Task Export_IncorrectOrder(Stream stream)
    {
        var bufferSize = Math.Max(1024, BufferSize);
        using var bufferedStream = new BufferedStream(stream, bufferSize);
        using ZipArchive zipArchive = new ZipArchive(bufferedStream, ZipArchiveMode.Create, false);
        string id = System.Environment.MachineName + ":" + Guid.NewGuid().ToString("N");
        var sessions = (await Storage.GetSessions()).ToList();
        var summaryJson = new { Id = id, Sessions = sessions };
        ZipArchiveEntry zipSummaryEntry = zipArchive.CreateEntry("Summary.json", CompressionLevel);
        using (var streamSummary = zipSummaryEntry.Open())
        {
            await System.Text.Json.JsonSerializer.SerializeAsync(streamSummary, summaryJson);
        }

        Stopwatch startAt = Stopwatch.StartNew();

        int totalActions = 0;
        foreach (var session in sessions)
        {
            ZipArchiveEntry zipSessionEntry = zipArchive.CreateEntry($"Session-{session.IdSession:n0}.json", CompressionLevel);
            using var streamSessionRaw = zipSessionEntry.Open();
            using var streamSession = new BufferedStream(streamSessionRaw, bufferSize);
            streamSession.Write(ArrayStart, 0, ArrayStart.Length);

            var summary = await Storage.GetActionsSummary(session.IdSession, null, null);
            bool isNext = false;
            foreach (var keyPath in summary)
            {
                var actions = await Storage.GetActionsByKeyPath(session.IdSession, keyPath.Key, int.MaxValue - 1, null, null, null);
                foreach (ActionDetailsWithCounters action in actions)
                {
                    // if (totalActions > 100) continue;
                    if (isNext)
                    {
                        streamSession.Write(NextSeparator, 0, NextSeparator.Length);
                    }
                    streamSession.Write(Tab, 0, Tab.Length);
                    await System.Text.Json.JsonSerializer.SerializeAsync(streamSession, action);
                    isNext = true;
                    totalActions++;
                    if (totalActions % 1000 == 0 && EnableDebugLog)
                    {
                        var p = Process.GetCurrentProcess();
                        AppendLog($"Actions Progress: {totalActions}. Memory: {p.WorkingSet64 / 1024:n0} Kb");
                    }
                }
            }
            streamSession.Write(ArrayEnd, 0, ArrayEnd.Length);
        }

        if (EnableDebugLog)
        {
            var p = Process.GetCurrentProcess();
            AppendLog($"Total Actions: {totalActions}. Memory: {p.WorkingSet64 / 1024:n0} Kb");
        }
        AppendLog($"Duration: {startAt.Elapsed}");

        ZipArchiveEntry zipLogEntry = zipArchive.CreateEntry("Log.log", CompressionLevel);
        using (var streamLog = zipLogEntry.Open())
        using (StreamWriter wr = new StreamWriter(streamLog))
        {
            wr.Write(Log);
        }
    }

    public static volatile bool EnableDebugLog = true;
    private StringBuilder DebugLog = new StringBuilder();

    private static readonly byte[] NextSeparator = new UTF8Encoding(false).GetBytes(",\n");
    private static readonly byte[] ArrayStart = new UTF8Encoding(false).GetBytes("[\n");
    private static readonly byte[] ArrayEnd = new UTF8Encoding(false).GetBytes("\n]");
    private static readonly byte[] Tab = new UTF8Encoding(false).GetBytes("\t");


    public string Log => DebugLog.ToString();

    void AppendLog(string line)
    {
        if (EnableDebugLog) DebugLog.AppendLine(line);
    }

}

public static class SqlInsightsExportImportExtensions
{
    public static void Export(this SqlInsightsExportImport export, string fileName)
    {
        var fullName = Path.GetFullPath(fileName);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullName));
        }
        catch
        {
        }

        using (FileStream fs = new FileStream(fullName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 32768))
        {
            export.Export(fs).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
#endif