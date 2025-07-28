using System;
using System.Collections.Concurrent;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Universe.SqlInsights.Shared;
using Universe.SqlInsights.SqlServerStorage;
using Universe.SqlTrace;

namespace Universe.SqlInsights.GenericInterceptor
{
    public static class ExperimentalMeasuredAction
    {
        private static long LogCounter;

        public static void PerformInternalAction(
            ISqlInsightsConfiguration config,
            ISqlInsightsStorage storage,
            SqlInsightsActionKeyPath keyPath,
            Action<string> action,
            ICrossPlatformLogger logger
        )
        {
            Perform(
                config,
                storage,
                config.HistoryConnectionString, 
                keyPath,
                action,
                logger
            );
        }

        public static void PerformApplicationAction(
            ISqlInsightsConfiguration config,
            ISqlInsightsStorage storage,
            SqlInsightsActionKeyPath keyPath,
            Action<string> action,
            ICrossPlatformLogger logger
        )
        {
            Perform(
                config,
                storage,
                config.ConnectionString, 
                keyPath, 
                action, 
                logger
                );
        }


        // TODO: ASYNC
        // TODO: Propagate storage 
        public static void Perform(
            ISqlInsightsConfiguration config,
            ISqlInsightsStorage storage,
            string traceableConnectionString,
            SqlInsightsActionKeyPath keyPath,
            Action<string> action,
            ICrossPlatformLogger logger
        )
        {
            // traceableConnectionString is either Storage's or AdventureWorks' DB

            string idAction = Guid.NewGuid().ToString("N");

            SqlTraceReader traceReader = new SqlTraceReader();
            traceReader.MaxFileSize = config.MaxTraceFileSizeKb;
            string appName = string.Format(config.SqlClientAppNameFormat, idAction);

            //SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(traceableConnectionString)
            //{
            //    ApplicationName = appName
            //};
            var csb = SqlTraceConfiguration.DbProvider.CreateConnectionStringBuilder();
            csb.ConnectionString = traceableConnectionString;
            csb["Application Name"] = appName;

            string connectionString = csb.ConnectionString;

            TraceRowFilter appFilter = TraceRowFilter.CreateByApplication(appName);
            traceReader.Start(traceableConnectionString /*config.ConnectionString*/ ,
                config.SqlTracesDirectory,
                TraceColumns.Application | TraceColumns.Sql,
                appFilter);

            Stopwatch startAt = Stopwatch.StartNew();
            CpuUsage.CpuUsage? cpuUsageAtStart = CpuUsage.CpuUsage.GetByThread();

            var logCounter = Interlocked.Increment(ref LogCounter);
            logger.LogInformation($"[ExperimentalMeasuredAction.Perform {logCounter:00000000}] BEFORE \"{keyPath}\" {connectionString}");
            Exception exception = null;
            try
            {
                action(connectionString);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            logger.LogInformation($"[ExperimentalMeasuredAction.Perform {logCounter:00000000}] AFTER \"{keyPath}\" ({(exception == null ? "OK" : $"{exception.Message}")}) {connectionString}");


            double duration = startAt.ElapsedTicks * 1000d / Stopwatch.Frequency;
            CpuUsage.CpuUsage? cpuUsage = CpuUsage.CpuUsage.GetByThread() - cpuUsageAtStart;

            TraceDetailsReport details = traceReader.ReadDetailsReport();
            SqlCounters sqlProfilerSummary = details.Summary;

            traceReader.Stop();
            traceReader.Dispose();

            config.DeleteTraceFile(traceReader.TraceFile);

            var actionDetailsWithCounters = SqlGenericInterceptor.StoreAction(
                config,
                storage, 
                logger,
                keyPath,
                duration,
                cpuUsage.GetValueOrDefault(),
                exception,
                details,
                SqlInsightsReport.Instance,
                false,
                SqlGenericInterceptor.FirstInvocationBehaviour.Ignore
            );

            // TODO: Return it from SqlGenericInterceptor.StoreAction(...)
            var stat = SqlInsightsReport.Instance.GetAverage(keyPath);

            StringBuilder log = new StringBuilder();
            log.AppendLine($"Detailed metrics for «{keyPath}»");
            if (actionDetailsWithCounters.BriefException != null)
            {
                log.AppendLine($"    Fail ...............: {actionDetailsWithCounters.BriefException}");
#if DEBUG
                log.AppendLine(actionDetailsWithCounters.ExceptionAsString);
#endif

            }

            log.AppendLine($"    Duration ...........: {duration:n1} milliseconds, average={stat?.AppDuration / stat?.Count:n1}, count={stat?.Count}");
            log.AppendLine($"    Cpu Usage ..........: [user] {cpuUsage?.UserUsage.TotalMicroSeconds / 1000d:n1} + [kernel] {cpuUsage?.KernelUsage.TotalMicroSeconds / 1000d:n1} milliseconds");
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

            // Console.WriteLine(log);
            logger?.LogInformation(log.ToString());
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

        private static ConcurrentDictionary<string, StatByAction> _Avg = new ConcurrentDictionary<string, StatByAction>();
        private static ConcurrentDictionary<string, StatByAction> _First = new ConcurrentDictionary<string, StatByAction>();
        class StatByAction
        {
            public double Duration;
            public long Count;
        }
    }
}