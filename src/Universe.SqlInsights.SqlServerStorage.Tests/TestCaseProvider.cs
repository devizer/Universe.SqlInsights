using System.Collections.Generic;
using System.Data.Common;
using System.IO;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public class TestCaseProvider
    {
        // private string ConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Database=SqlServerSqlInsightsStorage_Tests; Integrated Security=SSPI";
        private static string TheConnectionString = "Data Source=(local);Database=SqlServerSqlInsightsStorage_Tests; Integrated Security=SSPI";

        public DbProviderFactory Provider { get; set; }
        public string ConnectionString { get; set; }

        public override string ToString()
        {
            return Path.GetFileNameWithoutExtension(Provider.GetType().Assembly.Location);
        }

        public static IEnumerable<TestCaseProvider> GetList()
        {
            yield return new TestCaseProvider()
            {
                Provider = System.Data.SqlClient.SqlClientFactory.Instance,
                ConnectionString = TheConnectionString
            };

            // yield break;

            yield return new TestCaseProvider()
            {
                Provider = Microsoft.Data.SqlClient.SqlClientFactory.Instance,
                ConnectionString = TheConnectionString
            };

        }

    }
}