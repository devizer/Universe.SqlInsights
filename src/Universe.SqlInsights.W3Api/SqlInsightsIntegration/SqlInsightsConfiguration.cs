using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Universe.SqlInsights.NetCore;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.W3Api.SqlInsightsIntegration
{
    public class SqlInsightsConfiguration : ISqlInsightsConfiguration
    {
        private IConfiguration Configuration { get; }

        public SqlInsightsConfiguration(IConfiguration configuration) : this()
        {
            Configuration = configuration;
        }

        public string AppName { get; } = "SqlInsights";
        public string HostId => Environment.MachineName;
        public bool Enabled { get; } = true;
        public bool MeasureSqlMetrics { get; } = true;
        public decimal AutoFlushDelay { get; } = 1000;

        private Lazy<string> _ReportFullFileName;
        public string ReportFullFileName => _ReportFullFileName.Value;
        public string SqlTracesDirectory { get; } = "C:\\Temp\\SqlInsights-Traces";

        public string ConnectionString =>
            Configuration.GetConnectionString("SqlInsights")
            ?? throw new InvalidOperationException("Needs SqlInsights ConnectionString");

        public string HistoryConnectionString => "server=(local);Initial Catalog=SqlInsights_v4;Integrated Security=SSPI;Application Name=SqlInsights";
        public string SqlClientAppNameFormat => $"{this.AppName}-{{0}}";
        public int MaxTraceFileSizeKb { get; } = 128 * 1024;
        public int LatestInmemoryDetailRows { get; } = 1;

        public bool DisposeByShellCommand { get; } = true;
        public string SharedSqlTracesDirectory { get; } = null;

        private SqlInsightsConfiguration()
        {
            _ReportFullFileName = new Lazy<string>(GetReportFillFileName);
        }

        private static string GetReportFillFileName()
        {
            var ret = Environment.GetEnvironmentVariable("SQLINSIGHTS_REPORT_FULLNAME");
            if (!string.IsNullOrEmpty(ret)) return ret;
            if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
                return "C:\\Temp\\SqlInsights-W3Api\\Report.txt";
            else
                return "/tmp/SqlInsights-W3Api/Report.txt";
        }
    }
    
    public class CustomGroupingActionFilter : IActionFilter
    {
        private KeyPathHolder KeyPathHolder;

        public CustomGroupingActionFilter(KeyPathHolder keyPathHolder)
        {
            KeyPathHolder = keyPathHolder;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            object action = context.RouteData.Values["action"];
            object controller = context.RouteData.Values["controller"];
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
                keys.Add(context.HttpContext.Request.Path);
            }
            keys.Add($"[{context.HttpContext.Request.Method}]");
            SqlInsightsActionKeyPath keyPath = new SqlInsightsActionKeyPath(keys.ToArray());

            KeyPathHolder.KeyPath = keyPath;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

    }

}