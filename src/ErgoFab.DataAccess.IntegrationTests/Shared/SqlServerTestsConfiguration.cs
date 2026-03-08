using System.Text;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

public class SqlServerTestsConfiguration : ISqlServerTestsConfiguration
{
    public static readonly SqlServerTestsConfiguration Instance = new SqlServerTestsConfiguration();
    // System
    public string MasterConnectionString { get; } = GetMasterConnectionString();

    // Microsoft (Trust Server Certificate=True;Encrypt=False)
    // public string MasterConnectionString { get; } = "Data Source=tcp:(local)\\dev_2019;Integrated Security=True;Pooling=true;Timeout=30;Trust Server Certificate=True;";
    public string Provider { get; } = GetEnvProvider();

    static string GetEnvProvider() => 
        Environment.GetEnvironmentVariable("ERGOFAB_SQL_PROVIDER") ?? "System";

    public string DbNamePrefix { get; } = "Ergo Fab";
    public string BackupFolder { get; } = GetErgoFabDbDataFolder();
    public string DatabaseDataFolder { get; } = GetErgoFabDbDataFolder();
    public string DatabaseLogFolder { get; } = GetErgoFabDbDataFolder();

    static string GetMasterConnectionString()
    {
        var raw = Environment.GetEnvironmentVariable("ERGOFAB_TESTS_MASTER_CONNECTIONSTRING");
        // TrustServerCertificate=True;?
        return
            string.IsNullOrEmpty(raw)
                ? "Data Source=(local);Integrated Security = SSPI;Pooling=true;Timeout=30;Encrypt=False;"
                : raw;

    }

    static string GetErgoFabDbDataFolder()
    {
        var explicitResult = Environment.GetEnvironmentVariable("ERGOFAB_TESTS_DATA_FOLDER");
        if (!string.IsNullOrEmpty(explicitResult)) return explicitResult;

        return null;
    }

}