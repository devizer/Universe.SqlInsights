namespace Universe.SqlInsights.Shared
{
    public interface ISqlInsightsConfiguration
    {
        string AppName { get; }
        string HostId { get; }
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