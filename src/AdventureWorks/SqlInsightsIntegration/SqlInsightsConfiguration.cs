using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Universe.SqlInsights.AspNetLegacy;
using Universe.SqlInsights.Shared;

namespace AdventureWorks.SqlInsightsIntegration
{
    public class SqlInsightsConfiguration : ISqlInsightsConfiguration
    {
        public string AppName { get; } = "AdventureWorks";
        public string HostId => Environment.MachineName;
        public bool Enabled { get; } = true;
        public bool MeasureSqlMetrics { get; } = true;
        public decimal AutoFlushDelay { get; } = 1000;
        public string ReportFullFileName { get; } = "C:\\Temp\\AdventureWorks-Dev-Report.txt";
        public string SqlTracesDirectory { get; } = "C:\\Temp\\AdventureWorks-Traces";
        public string ConnectionString { get; } = ConfigurationManager.ConnectionStrings["AdventureWorks"].ToString();
        public string HistoryConnectionString => "server=(local);Initial Catalog=SqlInsights Local Warehouse; Integrated Security=SSPI";
        public string SqlClientAppNameFormat => $"{this.AppName}-{{0}}";
        public int MaxTraceFileSizeKb { get; } = 128 * 1024;
        public int LatestInmemoryDetailRows { get; } = 1;

        public bool DisposeByShellCommand { get; } = true;
        public string SharedSqlTracesDirectory { get; } = null;

        public static readonly SqlInsightsConfiguration Instance = new SqlInsightsConfiguration();
        private SqlInsightsConfiguration() {}
    }

    public class CustomGroupingActionFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var keyPath = GetGroupingKey(filterContext);
            if (keyPath != null)
                LegacySqlProfilerContext.Instance.ActionKeyPath = keyPath;
        }

        private static SqlInsightsActionKeyPath GetGroupingKey(ActionExecutingContext filterContext)
        {
            SqlInsightsActionKeyPath ret = null;
            if (!filterContext.IsChildAction)
            {
                object mvcController = filterContext.RouteData.Values["controller"];
                object mvcAction = filterContext.RouteData.Values["action"];
                string httpMethod = filterContext?.HttpContext?.Request?.HttpMethod;

                ret = new SqlInsightsActionKeyPath(
                    "ASP.NET MVC",
                    mvcController == null || (string) mvcController == "" ? "<unknown controller>" : (string) mvcController,
                    mvcAction == null || (string) mvcAction == "" ? "<unknown action>" : (string) mvcAction,
                    string.IsNullOrEmpty(httpMethod) ? "<unknown method>" : $"[{httpMethod}]"
                );

                var debug = filterContext.RouteData.Values.Keys.OrderBy(x => x);
                Debug.WriteLine("ASP.NET MVC Action Key Path: " + LegacySqlProfilerContext.Instance.ActionKeyPath +
                                ", keys: " + string.Join(", ", debug.ToArray()));
            }
            // else child actions does not override grouping key 

            return ret;
        }

        public void OnActionExecuted(ActionExecutedContext filterContext)
        {
        }
    }

    // for web forms and static files the grouping key is Path
    public class ProfilerInsightsModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BindSqlInsights(SqlInsightsConfiguration.Instance);
            SqlInsightsReport.AutoFlush(SqlInsightsConfiguration.Instance.ReportFullFileName, (int) SqlInsightsConfiguration.Instance.AutoFlushDelay);

            context.PostMapRequestHandler += (sender, args) =>
            {
                var keyPath = GetGroupingKey(context);
                if (keyPath != null)
                    LegacySqlProfilerContext.Instance.ActionKeyPath = keyPath;
            };
        }

        public void Dispose()
        {
        }
        
        private SqlInsightsActionKeyPath GetGroupingKey(HttpApplication app)
        {
            List<string> keyPath = new List<string>(3);

            IHttpHandler handler = app.Context?.Handler;
            if (handler != null)
            {
                keyPath.Add("ASP.NET Web Forms");
                keyPath.Add(handler.GetType().ToString());
            }
            else 
                keyPath.Add("ASP.NET Misc");

            var path = app.Request?.Url?.AbsolutePath;
            if (path != null) keyPath.Add(path);
            
            if (keyPath.Count == 0) keyPath.Add("Generic Http Request");

            var httpMethod = app.Request?.HttpMethod;
            if (httpMethod != null)
                keyPath.Add($"[{httpMethod}]");

            return new SqlInsightsActionKeyPath(keyPath.ToArray());
        }
    }

    public class ConnectionStringBuilder
    {
        public static string BuildConnectionString()
        {
            string actionId = LegacySqlProfilerContext.Instance.ActionId;
            string staticConnectionString = ConfigurationManager.ConnectionStrings["AdventureWorks"].ToString();
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(staticConnectionString);
            builder.ApplicationName = string.Format(SqlInsightsConfiguration.Instance.SqlClientAppNameFormat, actionId);
            return builder.ConnectionString;
        }
    }

}