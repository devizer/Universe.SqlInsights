using ErgoFab.DataAccess.IntegrationTests.Library;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

public class SqlServerTestsConfiguration : ISqlServerTestsConfiguration
{
    public static readonly SqlServerTestsConfiguration Instance = new SqlServerTestsConfiguration();
    // System
    // public string MasterConnectionString { get; } = "Data Source=tcp:(local);Integrated Security=True;Pooling=true;Timeout=30;TrustServerCertificate=True;";

    // Microsoft (Trust Server Certificate=True;Encrypt=False)
    public string MasterConnectionString { get; } = "Data Source=tcp:(local)\\dev_2019;Integrated Security=True;Pooling=true;Timeout=30;Trust Server Certificate=True;";
    public string Provider { get; } = "Microsoft";


    public string DbName { get; } = "Ergo Fab";
    public string BackupFolder { get; } = "W:\\Temp\\Integration Tests DEV2019\\Backups";
    public string DatabaseDataFolder { get; } = "W:\\Temp\\Integration Tests DEV2019";
    public string DatabaseLogFolder { get; } = "W:\\Temp\\Integration Tests DEV2019";


}