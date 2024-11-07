namespace Universe.SqlInsights.NUnit;

public static class ConnectionStringExtensions
{
    public static string GetDatabaseName(this SqlServerTestDbManager man, string connectionString)
    {
        var b = man.CreateDbProviderFactory().CreateConnectionStringBuilder();
        b.ConnectionString = connectionString;
        var dbName = b["Initial Catalog"]?.ToString();
        return dbName;
    }

}