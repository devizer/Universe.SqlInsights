using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.NUnitPipeline;
using Universe.SqlInsights.NUnit;

namespace ErgoFab.DataAccess.IntegrationTests.Shared
{
    public class TestDatabaseNameProvider : ITestDatabaseNameProvider
    {
        private static volatile int TestCounter = 0;
        public async Task<string> GetNextTestDatabaseName()
        {
            SqlServerTestDbManager testManager = NUnitPipelineConfiguration.GetService<SqlServerTestDbManager>();
            var counter = Interlocked.Increment(ref TestCounter);
            string dbPrefix = $"{testManager.SqlTestsConfiguration.DbName} Test {DateTime.Now.ToString("yyyy-MM-dd")}";
            return $"{dbPrefix} {counter:00000} {Guid.NewGuid():N}";
            var allNames = await testManager.GetDatabaseNames();
            var setNames = allNames.ToHashSet();
            var startFrom = Interlocked.Increment(ref TestCounter);
            for (int i = startFrom; i < 10000; i++)
            {
                string ret = $"{dbPrefix} {i:0000}";
                if (!setNames.Contains(ret))
                {
                    // TestCounter = i;
                    return ret;
                }
            }

            return $"{dbPrefix} {Guid.NewGuid():N}";
        }

    }
}
