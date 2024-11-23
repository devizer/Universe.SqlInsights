using ErgoFab.DataAccess.IntegrationTests.Shared;
using Microsoft.Data.SqlClient;
using Universe;
using Universe.NUnitPipeline;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;
using Universe.SqlInsights.Shared;
using Universe.SqlServerJam;

namespace Tracking;

public class ErgoFabSqlInsightsConfiguration : ISqlInsightsConfiguration
{
    public string AppName => "ErgoFab Data Access Integration Tests";
    public string HostId => Environment.MachineName;
    public bool Enabled => true;
    public bool MeasureSqlMetrics => true;
    public decimal AutoFlushDelay => 1000;

    public string ReportFullFileName => _ReportFullFileName.Value;
    public string SqlTracesDirectory => _IsServerHostOnWindows.Value ? (SystemDriveAccess.WindowsSystemDrive + "Temp\\SqlInsights-Traces") : "/tmp/SqlInsights-Traces";

    // Same as
    //    ISqlServerTestsConfiguration.MasterConnectionString
    //    e.g. SqlServerTestsConfiguration.MasterConnectionString
    public string ConnectionString => NUnitPipelineConfiguration.GetService<ISqlServerTestsConfiguration>().MasterConnectionString;

    // TODO: some shared DB on shared Server
    public string HistoryConnectionString => GetHistoryConnectionString();

    private string GetHistoryConnectionString()
    {
        var ret = Environment.GetEnvironmentVariable("ERGOFAB_TESTS_HISTORY_CONNECTIONSTRING");
        if (string.IsNullOrEmpty(ret))
            ret = "Server=(local);Encrypt=False;Initial Catalog=SqlInsights Local Warehouse;Integrated Security=SSPI";

        return ret;
    }

    public int MaxTraceFileSizeKb => 1024 * 1024;
    public string SqlClientAppNameFormat => $"{this.AppName} {{0}}";
    public bool DisposeByShellCommand => true;
    public string SharedSqlTracesDirectory { get; } = null;

    public ErgoFabSqlInsightsConfiguration()
    {
        _IsServerHostOnWindows = new Lazy<bool>(() =>
        {
            // TODO: Use Proper provider
            Microsoft.Data.SqlClient.SqlConnection cnn = new SqlConnection(ConnectionString);
            return cnn.Manage().IsWindows;
        });

        _ReportFullFileName = new Lazy<string>(GetReportFillFileName);
    }

    private Lazy<bool> _IsServerHostOnWindows;
    private Lazy<string> _ReportFullFileName;

    private static string GetReportFillFileName()
    {
        var ret = Environment.GetEnvironmentVariable("ERGOFAB_TESTS_REPORT_FULLNAME");
        if (!string.IsNullOrEmpty(ret)) return ret;
        if (CrossInfo.ThePlatform == CrossInfo.Platform.Windows)
            return SystemDriveAccess.WindowsSystemDrive + "Temp\\ErgFab Tests Report.txt";
        else
            return "/tmp/ErgFab Tests Report.txt";
    }
}