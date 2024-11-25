using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using Universe.CpuUsage;
using Universe.NUnitPipeline;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;
using Universe.SqlInsights.GenericInterceptor;
using Universe.SqlInsights.Shared;
using Universe.SqlTrace;

namespace Universe.SqlInsights.NUnit;

public partial class SqlServerTestCapturePipeline
{
    public static readonly string Title = "SQL Server Test Capture Pipeline";
    private static readonly object Sync = new object();

    public static void OnStart(NUnitStage stage, ITest test)
    {
        if (stage.NUnitActionAppliedTo != NUnitActionAppliedTo.Test) return;

        // var dbDefinishion = test.GetPropertyOrAdd<IDatabaseDefinition>("DatabaseDefinition", null);
        List<TestDbConnectionString> found = TestArgumentReflection.FindTestDbConnectionStrings(test.Arguments);
        PipelineLog.LogTrace(
            $"[SqlServerTestCapturePipeline.OnStart] Test='{test.Name}' TestDbConnectionString Count: {found.Count}, PostponedCount: {found.Count(x => x.Postponed)}{Environment.NewLine}");
        TestDbConnectionString testDbConnectionString = found.FirstOrDefault();
        if (testDbConnectionString != null)
        {
            // Start Trace
            string idTraceAction = Guid.NewGuid().ToString("N");

            var sqlServerTestsConfiguration = NUnitPipelineConfiguration.GetService<ISqlServerTestsConfiguration>();
            var sqlInsightsConfiguration = NUnitPipelineConfiguration.GetService<ISqlInsightsConfiguration>();
            var sqlInsightsStorage = NUnitPipelineConfiguration.GetService<ISqlInsightsStorage>();

            // TODO: It need to inject idTraceAction into application name
            SqlServerTestDbManager man = new SqlServerTestDbManager(sqlServerTestsConfiguration);

            // Starting capturing
            var appName = string.Format(sqlInsightsConfiguration.SqlClientAppNameFormat, idTraceAction);
            var master = sqlInsightsConfiguration.ConnectionString;
            SqlTraceReader traceReader = new SqlTraceReader();
            traceReader.MaxFileSize = sqlInsightsConfiguration.MaxTraceFileSizeKb;
            TraceRowFilter appFilter = TraceRowFilter.CreateByApplication(appName);
            traceReader.Start(master,
                sqlInsightsConfiguration.SqlTracesDirectory,
                TraceColumns.Application | TraceColumns.Sql,
                appFilter);

            // Patch App Name
            var connectionStringBuilder = man.CreateDbProviderFactory().CreateConnectionStringBuilder();
            connectionStringBuilder.ConnectionString = testDbConnectionString.ConnectionString;
            connectionStringBuilder["Application Name"] = appName;
            testDbConnectionString.ConnectionString = connectionStringBuilder.ConnectionString;

            lock (Sync)
            {
                if (null == test.GetPropertyOrAdd<TraceTestState>(nameof(TraceTestState), null))
                {
                    var traceTestState = new TraceTestState()
                    {
                        ActionId = idTraceAction,
                        TraceReader = traceReader,
                        Stopwatch = Stopwatch.StartNew(),
                        ThreadIdOnStart = Thread.CurrentThread.ManagedThreadId,
                        CpuUsageOnStart = CpuUsage.CpuUsage.GetByThread(),
                        CpuUsageAsyncWatcher = new CpuUsageAsyncWatcher(),
                        Finished = false,
                    };
                    var _ = test.GetPropertyOrAdd<TraceTestState>(nameof(TraceTestState), t => traceTestState);
                    PipelineLog.LogTrace("[SqlServerTestCapturePipeline] (1) Trace and (2) Cpu Usage Watcher are started");
                }
            }
        }

    }

    public static void OnFinish(NUnitStage stage, ITest test)
    {
        if (stage.NUnitActionAppliedTo == NUnitActionAppliedTo.Assembly)
        {
            Stopwatch startAt = Stopwatch.StartNew();
            var reportFullFileName = NUnitPipelineConfiguration.GetService<ISqlInsightsConfiguration>().ReportFullFileName;
            SqlInsightsReport.Instance.Flush(reportFullFileName, startAt);
            return;
        }

        if (stage.NUnitActionAppliedTo != NUnitActionAppliedTo.Test) return;
        TraceTestState traceTestState;

        lock (Sync)
        {
            traceTestState = test.GetPropertyOrAdd<TraceTestState>(nameof(TraceTestState), null);
        }
        traceTestState?.TraceReader.Stop();

        if (traceTestState == null || traceTestState.Finished) return;

        // Stop Cpu Usage
        TimeSpan elapsed = traceTestState.Stopwatch.Elapsed;
        traceTestState.CpuUsageAsyncWatcher.Stop();
        traceTestState.Finished = true;
        PipelineLog.LogTrace("[SqlServerTestCapturePipeline:OnFinish] Cpu Usage Watcher is stopped");

        // Stop Capturing
        var traceReader = traceTestState.TraceReader;
        TraceDetailsReport details = traceReader.ReadDetailsReport();
        SqlCounters sqlProfilerSummary = details.Summary;
        traceReader.Stop();
        traceReader.Dispose();
        PipelineLog.LogTrace("[SqlServerTestCapturePipeline:OnFinish] Trace is stopped");

        // Calculate [bool] hasCpuUsage and [CpuUsage] finalCpuUsage
        var asyncTotals = traceTestState.CpuUsageAsyncWatcher.Totals;
        CpuUsage.CpuUsage? syncCpuUsage = null;
        if (traceTestState.CpuUsageOnStart.HasValue && Thread.CurrentThread.ManagedThreadId == traceTestState.ThreadIdOnStart)
        {
            var cpuUsageAtEnd = CpuUsage.CpuUsage.GetByThread();
            if (cpuUsageAtEnd.HasValue)
            {
                syncCpuUsage = cpuUsageAtEnd.Value - traceTestState.CpuUsageOnStart.Value;
            }
        }

        bool hasCpuUsage = syncCpuUsage.HasValue || asyncTotals.Count > 0;
        var finalCpuUsage = asyncTotals.GetSummaryCpuUsage() + syncCpuUsage.GetValueOrDefault();

        var sqlServerTestsConfiguration = NUnitPipelineConfiguration.GetService<ISqlServerTestsConfiguration>();
        var sqlInsightsConfiguration = NUnitPipelineConfiguration.GetService<ISqlInsightsConfiguration>();
        var sqlInsightsStorage = NUnitPipelineConfiguration.GetService<ISqlInsightsStorage>();

        sqlInsightsConfiguration.DeleteTraceFile(traceReader.TraceFile);

        // Store
        var keyPath = new SqlInsightsActionKeyPath(stage.StructuredFullName);
        var outcomeString = TestContext.CurrentContext.Result.Outcome.ToString();
        bool hasException =
            outcomeString.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0
            || outcomeString.IndexOf("Failure", StringComparison.OrdinalIgnoreCase) >= 0;

        string errorString =
            hasException
                ? TestContext.CurrentContext.Result.Message
                : null;

        Exception exception =
            errorString == null
                ? null
                : new TestFail($"Test {outcomeString}. {errorString}");

        var actionDetailsWithCounters = SqlGenericInterceptor.StoreAction(
            sqlInsightsConfiguration, 
            sqlInsightsStorage, 
            ConsoleCrossPlatformLogger.Instance, 
            keyPath,
            elapsed.TotalSeconds * 1000,
            finalCpuUsage,
            exception,
            details,
            SqlInsightsReport.Instance,
            false,
            SqlGenericInterceptor.FirstInvocationBehaviour.Store
        );
    }
}