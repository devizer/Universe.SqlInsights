using System;
using Microsoft.Extensions.Configuration;
using Universe.SqlInsights.Shared;

namespace Universe.SqlInsights.W3Api.SqlInsightsIntegration
{
    public class SqlInsightsConfiguration : ISqlInsightsConfiguration
    {
        private IConfiguration Configuration { get; }

        public SqlInsightsConfiguration(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public string AppName { get; } = "AdventureWorks";
        public bool Enabled { get; } = true;
        public bool MeasureSqlMetrics { get; } = true;
        public decimal AutoFlushDelay { get; } = 1000;
        public string ReportFullFileName { get; } = "C:\\Temp\\AdventureWorks-Dev-Report.txt";
        public string SqlTracesDirectory { get; } = "C:\\Temp\\AdventureWorks-Traces";

        public string ConnectionString => Configuration.GetConnectionString("SqlInsights") 
                                          ?? throw new InvalidOperationException(
                                              "Needs SqlInsights ConnectionString");
        
        public string HistoryConnectionString => "server=(local);Initial Catalog=SqlInsights_v4; Integrated Security=SSPI";
        public string SqlClientAppNameFormat => $"{this.AppName}-{{0}}";
        public int MaxTraceFileSizeKb { get; } = 128 * 1024;
        public int LatestInmemoryDetailRows { get; } = 1;

        private SqlInsightsConfiguration() {}
    }
}