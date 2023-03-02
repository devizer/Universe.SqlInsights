﻿using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using Universe.SqlInsights.Shared;
using Universe.SqlTrace;
using Universe.CpuUsage;

namespace Universe.SqlInsights.NetCore
{

    public class ExperimentalMeasuredAction
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

            action(connectionString);

            double duration = startAt.ElapsedTicks * 1000d / Stopwatch.Frequency;
            CpuUsage.CpuUsage? cpuUsage = CpuUsage.CpuUsage.GetByThread() - cpuUsageAtStart;
            
            TraceDetailsReport details = traceReader.ReadDetailsReport();
            var sqlProfilerSummary = details.Summary;
            
            traceReader.Stop();
            traceReader.Dispose();

            StringBuilder log = new StringBuilder();
            log.AppendLine($"Detailed metrics for «{keyPath}»");
            log.AppendLine($"    Duration ...........: {duration:n1} milliseconds");
            log.AppendLine($"    Cpu Usage ..........: [user] {cpuUsage?.UserUsage.TotalMicroSeconds/1000d:n1} + [kernel] {cpuUsage?.KernelUsage.TotalMicroSeconds/1000d:n1} milliseconds");
            log.AppendLine($"    Sql Summary ........: {sqlProfilerSummary}");
            int n = 0;
            foreach (SqlStatementCounters detail in details)
            {
                log.AppendLine($"#{++n} {detail.Counters}");
                var sqlCode = string.IsNullOrEmpty(detail.Sql) ? detail.SpName : detail.Sql;
                sqlCode = sqlCode.TrimStart(new[] { '\r', '\n' });
                log.AppendLine($"{sqlCode}");
            }
            
            Console.WriteLine(log);
        }
    }
}