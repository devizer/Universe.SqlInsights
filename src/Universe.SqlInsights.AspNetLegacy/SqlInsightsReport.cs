using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Universe.SqlInsights.Shared;
using Universe.SqlInsights.SqlServerStorage;

namespace Universe.SqlInsights.AspNetLegacy
{
    public class SqlInsightsReport
    {
        public static readonly SqlInsightsReport Instance = new SqlInsightsReport();
        private IDictionary<SqlInsightsActionKeyPath, ActionSummaryCounters> First = new ConcurrentDictionary<SqlInsightsActionKeyPath, ActionSummaryCounters>();
        private IDictionary<SqlInsightsActionKeyPath, ActionSummaryCounters> Avg = new ConcurrentDictionary<SqlInsightsActionKeyPath, ActionSummaryCounters>();
        private object Sync = new object();
        public Guid TimeStamp { get; private set; } = Guid.NewGuid();

        private static readonly Stopwatch AppStartedAt = Stopwatch.StartNew();
        private static readonly object SyncUptime = new object();

        public static TimeSpan Uptime
        {
            get
            {
                lock (SyncUptime) return AppStartedAt.Elapsed;
            }
        }

        public List<ActionSummaryCounters> AsList()
        {
            lock (Sync)
                return Avg.Values.ToList();
        }

        public ConsoleTable AsConsoleTable(Func<ActionSummaryCounters, double> orderSelector = null)
        {
            orderSelector = orderSelector ?? (x => (double) x.AppDuration);
            var list = AsList().OrderByDescending(orderSelector);
            var header = new List<string>()
            {
                "Request",
                "-Count", "-Errs", "-Length",
                "-Duration", "-CPU", "-User", "-Kernel"
            };

            if (LegacySqlProfiler.SqlInsightsConfiguration.MeasureSqlMetrics)
            {
                header.AddRange(new []
                {
                    "-SQL Reqs",
                    "-SQL Errs",
                    "-SQL Duration",
                    "-SQL CPU",
                    "-SQL Reads",
                    "-SQL Writes",
                });
            }

            var ret = new ConsoleTable(header);

            Func<double, long, string> format = (val, count) =>
            {
                return (val / count).ToString("f2");
            };

            foreach (var row in list)
            {
                var actionCount = row.Count;
                double l = (row.ResponseLength) / (double) actionCount;
                string length = row.ResponseLength == 0 ? "" : (l.ToString("f1") + "B");
                if (l > 1024) length = Math.Round(l / 1024d, 2).ToString("f1") + "K";
                else if (l > 1024*1024) length = Math.Round(l / 1024d/1024d, 2).ToString("f1") + "M";

                var tableRow = new List<object>()
                {
                    row.Key,
                    row.Count,
                    row.RequestErrors == 0 ? "" : row.RequestErrors.ToString("0"),
                    length,
                    format(row.AppDuration, actionCount),
                    format(row.AppKernelUsage + row.AppUserUsage, actionCount),
                    format(row.AppUserUsage, actionCount),
                    format(row.AppKernelUsage, actionCount),
                };

                if (LegacySqlProfiler.SqlInsightsConfiguration.MeasureSqlMetrics)
                {
                    Func<long?, string, string> formatNumber = (number, formatString) =>
                    {
                        if (number == null || number.Value == 0) return "";
                        return ((double)number.Value / actionCount).ToString(formatString);
                    };

                    tableRow.AddRange(new object[]
                    {
                        formatNumber(row?.SqlCounters.Requests, "f1"),
                        row?.SqlErrors.ToString("f0"),
                        formatNumber(row?.SqlCounters.Duration, "f2"),
                        formatNumber(row?.SqlCounters.CPU, "f2"),
                        formatNumber(row?.SqlCounters.Reads, "f2"),
                        formatNumber(row?.SqlCounters.Writes, "f2"),
                    });
                }

                ret.AddRow(tableRow);
            }

            return ret;
        }

        public void Add(ActionDetailsWithCounters req)
        {
            if (!LegacySqlProfiler.SqlInsightsConfiguration.Enabled)
                return;

            if (req.Key == null) return;

            TimeStamp = Guid.NewGuid();
            var asSummary = req.AsSummary();

            bool isFirst;
            lock (Sync)
            {
                ActionSummaryCounters first;
                if (!First.TryGetValue(req.Key, out first))
                {
                    First[req.Key] = asSummary;
                    return;
                }

                ActionSummaryCounters sum;
                if (!Avg.TryGetValue(req.Key, out sum))
                {
                    Avg[req.Key] = asSummary;
                    isFirst = true;
                }
                else
                {
                    sum.Add(asSummary);
                    isFirst = false;
                }
            }

            if (!isFirst)
            {
                var hcs = LegacySqlProfiler.SqlInsightsConfiguration.HistoryConnectionString;
                if (hcs != null)
                {
                    var history = new SqlServerSqlInsightsStorage(hcs);
                    history.AddAction(req);
                }
            }
        }

        public bool AddResponseLength(SqlInsightsActionKeyPath requestKey, long totalWrittenBytes)
        {
            lock (Sync)
            {
                ActionSummaryCounters first, avg;
                First.TryGetValue(requestKey, out first);
                Avg.TryGetValue(requestKey, out avg);
                if (first != null && avg == null)
                {
                    first.ResponseLength = (first.ResponseLength) + totalWrittenBytes;
                    TimeStamp = Guid.NewGuid();
                    return true;
                }

                if (avg != null)
                {
                    avg.ResponseLength = (avg.ResponseLength) + totalWrittenBytes;
                    TimeStamp = Guid.NewGuid();
                    return true;
                }

                return false;
            }
        }

        public static void AutoFlush()
        {
            Thread t = new Thread(() =>
            {
                Guid prev = SqlInsightsReport.Instance.TimeStamp;
                while (true)
                {
                    var next = SqlInsightsReport.Instance.TimeStamp;
                    if (!next.Equals(prev))
                    {
                        Stopwatch startAt = Stopwatch.StartNew();
                        Instance.Flush(LegacySqlProfiler.SqlInsightsConfiguration.ReportFullFileName, startAt);
                    }

                    prev = next;
                    Thread.Sleep((int) LegacySqlProfiler.SqlInsightsConfiguration.AutoFlushDelay);
                }
            }) { IsBackground = true, Name = "Profiler Flushing Thread"};

            t.Start();
        }

        public void Flush(string fileName, Stopwatch reportStartedAt = null)
        {
            var str = AsConsoleTable().ToString();
            double msecGenerated = reportStartedAt != null ? reportStartedAt.GetMilliseconds() : 0;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }
            catch
            {
            }

            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 8192))
                using (StreamWriter w = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    w.WriteLine(str);
                    if (reportStartedAt != null)
                    {
                        var msecWritten = reportStartedAt.GetMilliseconds();
                        w.WriteLine(Environment.NewLine + Environment.NewLine + $"Report generated during {msecGenerated:f2}" +
                            $" and written to this file during {(msecWritten - msecGenerated):f2} msec" +
                            Environment.NewLine + $"Legend: " + Environment.NewLine +
                            $" * Left number is an avg duration in milliseconds" + Environment.NewLine +
                            $" * Right number is an avg CPU usage in milliseconds" + Environment.NewLine +
                            "Warning: " + Environment.NewLine +
                            "   CPU usage in IIS apps doesn't work properly on desktop windows family" +
                            Environment.NewLine + Environment.NewLine
                            + $"This report Generated at {DateTime.Now}. App uptime is {Uptime}");
                    }
                }
            }
            catch
            {
            }

            var reportFile = Path.Combine(
                Path.GetDirectoryName(fileName),
                Path.GetFileNameWithoutExtension(fileName) + ".json"
            );

#if false
            try
            {
                using (FileStream fs = new FileStream(reportFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 8192))
                using (StreamWriter w = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    w.WriteLine(ToJson());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Flushing of performance report to file {0} postponsed. Reason is below{1}{2}",
                    reportFile, Environment.NewLine, ex);
            }
#endif
        }

        public string ToJson()
        {
            var list = AsList();
            SharedProfilerReport report = new SharedProfilerReport()
            {
                Counters = list.ToList(),
                GeneratedAt = DateTime.UtcNow,
                IsSqlMetricsIncluded = LegacySqlProfiler.SqlInsightsConfiguration.MeasureSqlMetrics,
            };

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var ret = JsonConvert.SerializeObject(report, Formatting.Indented, settings);
            return ret;
        }

    }

    public static class ProfilerReportExtensions
    {
        // returns summary by actions only, without latest actions
        public static IEnumerable<ActionSummaryCounters> AsActionsSummary(this SqlInsightsReport report)
        {
            return report.AsList().Select(x => new ActionSummaryCounters()
            {
                Count = x.Count,
                Key = x.Key,
                AppDuration = x.AppDuration,
                RequestErrors = x.RequestErrors,
                ResponseLength = x.ResponseLength,
                SqlCounters = x.SqlCounters,
                SqlErrors = x.SqlErrors,
                AppKernelUsage = x.AppKernelUsage,
                AppUserUsage = x.AppUserUsage,
            });
        }
    }
}