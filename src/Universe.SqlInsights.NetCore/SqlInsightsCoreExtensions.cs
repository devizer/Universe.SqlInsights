using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Universe.CpuUsage;
using Universe.SqlInsights.Shared;
using Universe.SqlTrace;

namespace Universe.SqlInsights.NetCore
{
    public class ActionIdHolder
    {
        public Guid Id { get; set; }
    }

    public class ExceptionHolder
    {
        public Exception Error { get; set; }
    }

    public class KeyPathHolder
    {
        public SqlInsightsActionKeyPath KeyPath { get; set; }
    }

    public static class SqlInsightsCoreExtensions
    {
        // Exception .NET Core 1.1+
        // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-5.0

        public static IApplicationBuilder ValidateSqlInsightsServices(this IApplicationBuilder app)
        {
            
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var serviceProvider = scope.ServiceProvider;
                var idHolder = serviceProvider.GetRequiredService<ActionIdHolder>();
                var keyPathHolder = serviceProvider.GetRequiredService<KeyPathHolder>();
                var config = serviceProvider.GetRequiredService<ISqlInsightsConfiguration>();
                var storage = serviceProvider.GetRequiredService<ISqlInsightsStorage>();
                var exceptionHolder = serviceProvider.GetRequiredService<ExceptionHolder>();
                var report = serviceProvider.GetRequiredService<SqlInsightsReport>();
            }

            return app;
        }
        
        public static IApplicationBuilder UseSqlInsights(this IApplicationBuilder app)
        {
            app.Use(middleware: async delegate(HttpContext context, Func<Task> next)
            {
                // var aboutRequest = $"{context.Request?.GetDisplayUrl()} {context.Request?.Method}, {context.TraceIdentifier}";
                // Console.WriteLine($"⚠ Processing {aboutRequest}");
                var serviceProvider = context.RequestServices;
                var idHolder = serviceProvider.GetRequiredService<ActionIdHolder>();
                var keyPathHolder = serviceProvider.GetRequiredService<KeyPathHolder>();
                idHolder.Id = Guid.NewGuid();
                var config = serviceProvider.GetRequiredService<ISqlInsightsConfiguration>();
                
                // DEBUG
                var storage = serviceProvider.GetRequiredService<ISqlInsightsStorage>();
                var anyAliveSession = storage.AnyAliveSession();
                if (!anyAliveSession)
                {
                    await next.Invoke();
                    return;
                }
                
                SqlTraceReader traceReader = new SqlTraceReader();
                traceReader.MaxFileSize = config.MaxTraceFileSizeKb;
                string appName = string.Format(config.SqlClientAppNameFormat, idHolder.Id.ToString("N"));
                TraceRowFilter appFilter = TraceRowFilter.CreateByApplication(appName);
                traceReader.Start(config.ConnectionString, 
                    config.SqlTracesDirectory, 
                    TraceColumns.Application | TraceColumns.Sql,
                    appFilter);

                CpuUsageAsyncWatcher watcher = new CpuUsageAsyncWatcher();
                Stopwatch stopwatch = Stopwatch.StartNew();
                var idThreadBefore = Thread.CurrentThread.ManagedThreadId;
                var cpuUsageBefore = CpuUsage.CpuUsage.GetByThread();
                
                await next.Invoke();

                try
                {
                    var idThreadAfter = Thread.CurrentThread.ManagedThreadId;
                    watcher.Stop();
                    TraceDetailsReport details = traceReader.ReadDetailsReport();
                    CpuUsage.CpuUsage watcherTotals = watcher.Totals.GetSummaryCpuUsage();
                    if (watcher.Totals.Count == 0 && idThreadAfter == idThreadBefore)
                    {
                        var cpuUsageAfter = CpuUsage.CpuUsage.GetByThread();
                        watcherTotals = watcherTotals + (cpuUsageAfter - cpuUsageBefore).GetValueOrDefault();
                    }

                    var keyPath = keyPathHolder.KeyPath;
                    if (keyPath == null)
                    {
                        var httpPath = context.Request?.Path;
                        var httpMethod = context.Request?.Method;
                        keyPath = new SqlInsightsActionKeyPath("Misc", httpPath, $"[{httpMethod}]");
                    }

                    var sqlSummary = details.Summary;
#if DEBUG && false
                Console.WriteLine($@"On EndRequest for «{keyPath}» ({idThreadBefore} -> {idThreadAfter}):
{((char)160)} Sql Side Details (columns are '{details.IncludedColumns}'): {sqlSummary}
{((char)160)} CPU usage including possible sync execution: {watcherTotals}
{((char)160)} App Side details: {watcher.ToHumanString()}");
#endif

                    traceReader.Stop();
                    traceReader.Dispose();

                    config.DeleteTraceFile(traceReader.TraceFile);

                    Exception lastError = serviceProvider.GetRequiredService<ExceptionHolder>().Error;

                    ActionDetailsWithCounters actionDetails = new ActionDetailsWithCounters()
                    {
                        AppName = config.AppName,
                        HostId = config.HostId,
                        Key = keyPath,
                        At = DateTime.UtcNow,
                        IsOK = lastError == null,
                        AppDuration = stopwatch.ElapsedTicks / (double) Stopwatch.Frequency * 1000d,
                        AppKernelUsage = watcherTotals.KernelUsage.TotalMicroSeconds / 1000L,
                        AppUserUsage = watcherTotals.UserUsage.TotalMicroSeconds / 1000L,
                    };

                    actionDetails.SqlStatements.AddRange(details.Select(x =>
                        new ActionDetailsWithCounters.SqlStatement()
                        {
                            Counters = x.Counters,
                            Sql = x.Sql,
                            SpName = x.SpName,
                            SqlErrorCode = x.SqlErrorCode,
                            SqlErrorText = x.SqlErrorText,
                        }));

                    // ERROR
                    SqlExceptionInfo sqlException = lastError.FindSqlError();
                    if (sqlException != null)
                    {
                        actionDetails.BriefSqlError = new BriefSqlError()
                        {
                            Message = sqlException.Message,
                            SqlErrorCode = sqlException.Number,
                        };
                    }

                    if (lastError != null)
                    {
                        actionDetails.ExceptionAsString = lastError.ToString();
                        actionDetails.BriefException = lastError.GetBriefExceptionKey();
                    }

                    SqlInsightsReport inMemoryReport = serviceProvider.GetRequiredService<SqlInsightsReport>();
                    bool canSummarize = inMemoryReport.Add(actionDetails);

                    if (canSummarize) // not a first call?
                    {
                        if (storage is ITraceableStorage traceableStorage)
                        {
                            ExperimentalMeasuredAction.Perform(
                                config,
                                new SqlInsightsActionKeyPath($"[{storage.GetType().Name}]", "AddAction()"),
                                connectionString =>
                                {
                                    var traceableStorage = storage as ITraceableStorage;
                                    traceableStorage.ConnectionString = connectionString;
                                    storage?.AddAction(actionDetails);
                                }
                            );
                        }
                        else
                        {
                            storage?.AddAction(actionDetails);
                        }
                    }

                    // Console.WriteLine($"⚠ Processed  {aboutRequest}");
                }
                catch (Exception ex)
                {
                    // TODO: (Error 2627 on AddAction)
                    // System.Data.SqlClient.SqlException (0x80131904): Violation of PRIMARY KEY constraint 'PK_SqlInsightsKeyPathSummary'. Cannot insert duplicate key in object 'dbo.SqlInsightsKeyPathSummary'. The duplicate key value is (Misc→/api/v1/SqlInsights/Summary→[OPTIONS], 0, 1, 3).
                    Console.WriteLine("ERROR on SqlInsights Middleware for .NET Core" + Environment.NewLine + ex);
                }

                // Console.WriteLine($"Cpu Usage by http request is {watcher.GetSummaryCpuUsage()} {context.Request.Path} {context.Request.Method}");
            });
            
            return app;
        }  
    }
}