using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Universe.SqlInsights.Shared;
using Universe.SqlTrace;
using CpuUsage = Universe.CpuUsage.CpuUsage;

namespace Universe.SqlInsights.NetCore
{

    public static class ExperimentalMeasuredAction
    {
        
        public static void Perform(
            ISqlInsightsConfiguration config,
            SqlInsightsActionKeyPath keyPath,
            Action<string> action
            )
        {
            string idAction = Guid.NewGuid().ToString("N");
            
            SqlTraceReader traceReader = new SqlTraceReader();
            traceReader.MaxFileSize = config.MaxTraceFileSizeKb;
            string appName = string.Format(config.SqlClientAppNameFormat, idAction);

            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(config.ConnectionString)
            {
                ApplicationName = appName
            };

            string connectionString = csb.ConnectionString;
            
            TraceRowFilter appFilter = TraceRowFilter.CreateByApplication(appName);
            traceReader.Start(config.ConnectionString, 
                config.SqlTracesDirectory, 
                TraceColumns.Application | TraceColumns.Sql,
                appFilter);
            
            Stopwatch startAt = Stopwatch.StartNew();
            CpuUsage.CpuUsage? cpuUsageAtStart = CpuUsage.CpuUsage.GetByThread();

            Exception exception = null;
            try
            {
                action(connectionString);
            }
            catch (Exception ex)
            {
                exception = ex;
            }


            double duration = startAt.ElapsedTicks * 1000d / Stopwatch.Frequency;
            CpuUsage.CpuUsage? cpuUsage = CpuUsage.CpuUsage.GetByThread() - cpuUsageAtStart;
            
            TraceDetailsReport details = traceReader.ReadDetailsReport();
            SqlCounters sqlProfilerSummary = details.Summary;
            
            traceReader.Stop();
            traceReader.Dispose();
            
            config.DeleteTraceFile(traceReader.TraceFile);

            ActionDetailsWithCounters actionDetailsWithCounters = new ActionDetailsWithCounters()
            {
                Key = keyPath,
                AppDuration = duration,
                AppKernelUsage = (cpuUsage?.KernelUsage.TotalSeconds).GetValueOrDefault(),
                AppUserUsage = (cpuUsage?.UserUsage.TotalSeconds).GetValueOrDefault(),
                AppName = config.AppName,
                HostId = config.HostId,
                At = DateTime.UtcNow,
                IsOK = exception == null,
                BriefException = null,
                BriefSqlError = null,
                ExceptionAsString = null,
                SqlStatements = details.Select(x => new ActionDetailsWithCounters.SqlStatement()
                {
                    Counters = x.Counters,
                    SpName = x.SpName,
                    Sql = x.Sql,
                    SqlErrorCode = x.SqlErrorCode,
                    SqlErrorText = x.SqlErrorText,
                }).ToList()
            };

            SqlExceptionInfo sqlException = exception.FindSqlError();
            if (sqlException != null)
            {
                actionDetailsWithCounters.BriefSqlError = new BriefSqlError()
                {
                    Message = sqlException.Message,
                    SqlErrorCode = sqlException.Number,
                };
            }

            if (exception != null)
            {
                actionDetailsWithCounters.ExceptionAsString = exception.ToString();
                actionDetailsWithCounters.BriefException = exception.GetBriefExceptionKey();
            }

            SqlInsightsReport.Instance.Add(actionDetailsWithCounters);

            StatByAction stat;
            if (!_First.TryGetValue(keyPath.ToString(), out stat))
            {
                stat = _First[keyPath.ToString()] = new StatByAction() { Count = 1, Duration = duration };
            }
            else
            {
                stat = _Avg.GetOrAdd(keyPath.ToString(), action => new StatByAction());
                stat.Count++;
                stat.Duration += duration;
            }

            StringBuilder log = new StringBuilder();
            log.AppendLine($"Detailed metrics for «{keyPath}»");
            if (actionDetailsWithCounters.BriefException != null)
                log.AppendLine($"    Fail ...............: {actionDetailsWithCounters.BriefException}");

            log.AppendLine($"    Duration ...........: {duration:n1} milliseconds, average={stat.Duration / stat.Count:n1}, count={stat.Count}");
            log.AppendLine($"    Cpu Usage ..........: [user] {cpuUsage?.UserUsage.TotalMicroSeconds/1000d:n1} + [kernel] {cpuUsage?.KernelUsage.TotalMicroSeconds/1000d:n1} milliseconds");
            log.AppendLine($"    Sql Summary ........: {sqlProfilerSummary}");
            int n = 0;
            foreach (SqlStatementCounters detail in details)
            {
                string prefix = n == 0 ? "┌─" : "├─";
                var header = $"    {prefix} Query #{++n} ";
                if (header.Length < 24) header += new string('.', 24 - header.Length);
                
                log.AppendLine($"{header}: {detail.Counters}");
                var sqlCode = string.IsNullOrEmpty(detail.Sql) ? detail.SpName : detail.Sql;
                sqlCode = sqlCode.TrimStart(new[] { '\r', '\n' });
                log.AppendLine($"{MultilinePadding(sqlCode, "    │ ")}");
            }
            
            Console.WriteLine(log);
        }

        static string MultilinePadding(string arg)
        {
            return MultilinePadding(arg, "   │");
        }
        static string MultilinePadding(string arg, string padding)
        {
            StringBuilder ret = new StringBuilder();
            StringReader rdr = new StringReader(arg);
            string line = null;
            while ((line = rdr.ReadLine()) != null) ret.Append(ret.Length == 0 ? "" : Environment.NewLine).Append(padding + line);
            return ret.ToString();
        }

        private static ConcurrentDictionary<string, StatByAction> _Avg = new ();
        private static ConcurrentDictionary<string, StatByAction> _First = new ();
        class StatByAction
        {
            public double Duration;
            public long Count;
        }
    }
}