using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

    public static class SqlInsightsCoreExtensions
    {

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

                object action = context.GetRouteValue("action");
                object controller = context.GetRouteValue("controller");

                SqlInsightsActionKeyPath keyPath = new SqlInsightsActionKeyPath(
                    "ASP.NET Core",
                    controller == null ? "<null>" : Convert.ToString(controller),
                    action == null ? "<null>" : Convert.ToString(action)
                );
                
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
                
                await next.Invoke();
                watcher.Stop();
                TraceDetailsReport details = traceReader.ReadDetailsReport();
                
                var sqlSummary = details.Summary;
                Console.WriteLine($@"On EndRequest for «{keyPath}»:
{((char)160)} Sql Side Details (columns are '{details.IncludedColumns}'): {sqlSummary}
{((char)160)} App Side details: {watcher.ToHumanString()}");
                
                traceReader.Stop();
                traceReader.Dispose();
                var watcherTotals = watcher.Totals.GetSummaryCpuUsage();

                Exception lastError = null;
                ActionDetailsWithCounters actionDetails = new ActionDetailsWithCounters()
                {
                    AppName = config.AppName,
                    HostId = Environment.MachineName,
                    Key = keyPath,
                    At = DateTime.Now,
                    IsOK = lastError == null,
                    AppDuration = stopwatch.ElapsedTicks / (double) Stopwatch.Frequency,
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

                SqlInsightsReport r = null;
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
}