using System.Collections.Generic;
using System.Data.Common;
using System.IO;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class TestCaseProvider
    {
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
            System.Data.SqlClient.SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder(TestEnv.TheConnectionString);
            builder.InitialCatalog = string.Format(TestEnv.DbNamePattern, Path.GetFileNameWithoutExtension(dbProviderFactory.GetType().Assembly.Location));
            return new TestCaseProvider()
            {
                Provider = dbProviderFactory,
                ConnectionString = builder.ConnectionString,
            };
        }
    }
}