using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Universe.SqlInsights.Shared;

#if NETSTANDARD2_0_OR_GREATER || NET461

public class SqlInsightsExportImport 
{
    private readonly ISqlInsightsStorage Storage;

    public SqlInsightsExportImport(ISqlInsightsStorage storage)
    {
        Storage = storage;
    }

    public async Task Export(Stream stream)
    {
        using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, false);
        string id = System.Environment.MachineName + ":" + Guid.NewGuid().ToString("N");
        var sessions = (await Storage.GetSessions()).ToList();
        var summaryJson = new { Id = id, Sessions = sessions };
        ZipArchiveEntry zipSummaryEntry = zipArchive.CreateEntry("Summary.json", CompressionLevel.Fastest);
        using (var streamSummary = zipSummaryEntry.Open())
        {
            await System.Text.Json.JsonSerializer.SerializeAsync(streamSummary, summaryJson);
        }

        foreach (var session in sessions)
        {
            ZipArchiveEntry zipSessionEntry = zipArchive.CreateEntry($"Session-{session.IdSession:n0}.json", CompressionLevel.Fastest);
            using var streamSessionRaw = zipSessionEntry.Open();
            using var streamSession = new BufferedStream(streamSessionRaw, 32768);
            var summary = await Storage.GetActionsSummary(session.IdSession, null, null);
            foreach (var keyPath in summary)
            {
                var actions = await Storage.GetActionsByKeyPath(session.IdSession, keyPath.Key, int.MaxValue - 1, null, null, null);
                foreach (ActionDetailsWithCounters action in actions)
                {
                    await System.Text.Json.JsonSerializer.SerializeAsync(streamSession, action);
                }
            }
        }
    }

    public static volatile bool EnableDebugLog = false;
    private static StringBuilder DebugLog = new StringBuilder();
    private static readonly object SyncLog = new object();

    public static string Log
    {
        get
        {
            lock (SyncLog) return DebugLog.ToString();
        }
    }

    static void AppendLog(string line)
    {
        if (EnableDebugLog) lock (SyncLog) DebugLog.AppendLine(line);
    }

    public class IdSessionAndAction
    {
        public int IdSession { get; set; }
        public ActionDetailsWithCounters Action { get; set; }

    }

}
#endif