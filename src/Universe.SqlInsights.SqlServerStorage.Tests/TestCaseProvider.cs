using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class TestCaseProvider
    {
        // private string ConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Database=SqlServerSqlInsightsStorage_Tests; Integrated Security=SSPI";
        // private static readonly string TheConnectionString = "Data Source=(local);Database=SqlServerSqlInsightsStorage_Tests; Integrated Security=SSPI";
        private static string TheConnectionString => string.IsNullOrEmpty(DbConnectionString) ? "Data Source=(local);Integrated Security=SSPI" : DbConnectionString;
        private static readonly string DbNamePattern = "SqlInsights Storage {0} Tests";

        // OPTIONAL
        public static string DbDataDir => Environment.GetEnvironmentVariable("SQLINSIGHTS_DATA_DIR");
        public static string DbConnectionString => Environment.GetEnvironmentVariable("SQLINSIGHTS_CONNECTION_STRING");

        public DbProviderFactory Provider { get; set; }
        public string ConnectionString { get; set; }

        public override string ToString()
        {
            return Path.GetFileNameWithoutExtension(Provider.GetType().Assembly.Location);
        }

        public static IEnumerable<TestCaseProvider> GetList()
        {
            yield return CreateTestCaseProviderByType(System.Data.SqlClient.SqlClientFactory.Instance);
            yield return CreateTestCaseProviderByType(Microsoft.Data.SqlClient.SqlClientFactory.Instance);
        }

        static TestCaseProvider CreateTestCaseProviderByType(DbProviderFactory dbProviderFactory)
        {
            System.Data.SqlClient.SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder(TheConnectionString);
            builder.InitialCatalog = string.Format(DbNamePattern, Path.GetFileNameWithoutExtension(dbProviderFactory.GetType().Assembly.Location));
            return new TestCaseProvider()
            {
                Provider = dbProviderFactory,
                ConnectionString = builder.ConnectionString,
            };
        }
    }
}