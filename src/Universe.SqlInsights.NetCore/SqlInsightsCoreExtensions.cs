using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Universe.CpuUsage;
using Universe.SqlInsights.AspNetLegacy;
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

    public static class SqlInsightsCoreExtensions
    {
        // Exception .NET Core 1.1+
        // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-5.0
        

        public static IApplicationBuilder UseSqlInsights(this IApplicationBuilder app)
        {
            app.Use(middleware: async delegate(HttpContext context, Func<Task> next)
            {
                var serviceProvider = context.RequestServices;
                var idHolder = serviceProvider.GetRequiredService<ActionIdHolder>();
                idHolder.Id = Guid.NewGuid();
                var config = serviceProvider.GetRequiredService<ISqlInsightsConfiguration>();
                
                // DEBUG
                var storage = serviceProvider.GetRequiredService<ISqlInsightsStorage>();

                
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
                
                var idThreadAfter = Thread.CurrentThread.ManagedThreadId;
                watcher.Stop();
                TraceDetailsReport details = traceReader.ReadDetailsReport();
                CpuUsage.CpuUsage watcherTotals = watcher.Totals.GetSummaryCpuUsage();
                if (watcher.Totals.Count == 0 && idThreadAfter == idThreadBefore)
                {
                    var cpuUsageAfter = CpuUsage.CpuUsage.GetByThread();
                    watcherTotals = watcherTotals + (cpuUsageAfter - cpuUsageBefore).GetValueOrDefault();
                }

                
                object action = context.GetRouteValue("action");
                object controller = context.GetRouteValue("controller");
                List<string> keys = new List<string>
                {
                    "ASP.NET Core",
                    controller == null ? "<null>" : Convert.ToString(controller),
                    action == null ? "<null>" : Convert.ToString(action)
                };

                if (action == null || controller == null)
                {
                    keys.Clear();
                    keys.Add("ASP.NET Core");
                    keys.Add(context.Request.Path);
                }
                keys.Add($"[{context.Request.Method}]");
                SqlInsightsActionKeyPath keyPath = new SqlInsightsActionKeyPath(keys.ToArray());

                var sqlSummary = details.Summary;
#if DEBUG                
                Console.WriteLine($@"On EndRequest for «{keyPath}» ({idThreadBefore} -> {idThreadAfter}):
{((char)160)} Sql Side Details (columns are '{details.IncludedColumns}'): {sqlSummary}
{((char)160)} CPU usage including possible sync execution: {watcherTotals}
{((char)160)} App Side details: {watcher.ToHumanString()}");
#endif
                
                traceReader.Stop();
                traceReader.Dispose();

                Exception lastError = serviceProvider.GetRequiredService<ExceptionHolder>().Error;
                
                ActionDetailsWithCounters actionDetails = new ActionDetailsWithCounters()
                {
                    AppName = config.AppName,
                    HostId = Environment.MachineName,
                    Key = keyPath,
                    At = DateTime.Now,
                    IsOK = lastError == null,
                    AppDuration = stopwatch.ElapsedTicks / (double) Stopwatch.Frequency * 1000d,
                    AppKernelUsage = watcherTotals.KernelUsage.TotalMicroSeconds / 1000L,
                    AppUserUsage = watcherTotals.UserUsage.TotalMicroSeconds / 1000L,
                };
                
                actionDetails.SqlStatements.AddRange(details.Select(x => new ActionDetailsWithCounters.SqlStatement()
                {
                    Counters = x.Counters,
                    Sql = x.Sql,
                    SpName = x.SpName,
                    SqlErrorCode = x.SqlErrorCode,
                    SqlErrorText = x.SqlErrorText,
                }));
                
                // ERROR
                SqlException sqlException = lastError.GetSqlError();
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
                

                SqlInsightsReport r = serviceProvider.GetRequiredService<SqlInsightsReport>();
                bool needSummarize = r.Add(actionDetails);
                
                if (needSummarize)
                {
                    storage?.AddAction(actionDetails);
                }

                // Console.WriteLine($"Cpu Usage by http request is {watcher.GetSummaryCpuUsage()} {context.Request.Path} {context.Request.Method}");
            });
            
            return app;
        }  
    }
    
    public class CustomExceptionFilter : IExceptionFilter
    {
        private readonly IModelMetadataProvider ModelMetadataProvider;
        private ExceptionHolder ErrorHolder;

        public CustomExceptionFilter(IModelMetadataProvider modelMetadataProvider, ExceptionHolder errorHolder)
        {
            ModelMetadataProvider = modelMetadataProvider;
            ErrorHolder = errorHolder;
        }

        public void OnException(ExceptionContext context)
        {
            ErrorHolder.Error = context.Exception;
        }
    }
}