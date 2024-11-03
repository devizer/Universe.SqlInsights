using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErgoFab.DataAccess.IntegrationTests.Library;

namespace ErgoFab.DataAccess.IntegrationTests.Shared
{
    public static class TestDatabaseNameProvider
    {
        private static volatile int TestCounter = 0;
        public static async Task<string> GetNextTestDatabaseName(this SqlServerTestDbManager testManager)
        {
            string dbPrefix = $"{testManager.SqlTestsConfiguration.DbName} Test {DateTime.Now.ToString("yyyy-MM-dd")}";
            return $"{dbPrefix} {Guid.NewGuid():N}";
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
