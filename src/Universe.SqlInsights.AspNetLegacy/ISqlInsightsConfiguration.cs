namespace Universe.SqlInsights.AspNetLegacy
{
    public interface ISqlInsightsConfiguration
    {
        string AppName { get; }
        bool Enabled { get; }
        bool MeasureSqlMetrics { get; }
        decimal AutoFlushDelay { get; }
        string ReportFullFileName { get; }
        string SqlTracesDirectory { get; }
        string ConnectionString { get; }
        string HistoryConnectionString { get; }
        int MaxTraceFileSizeKb { get; }
        string SqlClientAppNameFormat { get; }
    }
}