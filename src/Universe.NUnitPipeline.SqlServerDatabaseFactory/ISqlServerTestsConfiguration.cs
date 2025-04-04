namespace Universe.NUnitPipeline.SqlServerDatabaseFactory
{
    public interface ISqlServerTestsConfiguration
    {
        string DbNamePrefix { get; }
        string MasterConnectionString { get; }
        // For local server folders might be configured.

        // for non local: fetched from SQL Server
        string BackupFolder { get; }
        string DatabaseDataFolder { get; }
        string DatabaseLogFolder { get; }
        string Provider { get;  } // Microsoft | System
        // Task<string> GetNextTestDatabaseName(SqlServerTestDbManager testManager);

    }
}