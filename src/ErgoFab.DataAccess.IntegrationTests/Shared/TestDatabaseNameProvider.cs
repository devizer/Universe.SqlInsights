using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Universe.NUnitPipeline;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;

namespace ErgoFab.DataAccess.IntegrationTests.Shared
{
    public class TestDatabaseNameProvider : ITestDatabaseNameProvider
    {
        private static volatile int TestCounter = 0;
        public async Task<string> GetNextTestDatabaseName()
        {
            ISqlServerTestsConfiguration sqlTestsConfiguration = NUnitPipelineConfiguration.GetService<ISqlServerTestsConfiguration>();
            var counter = Interlocked.Increment(ref TestCounter);
            string dbPrefix = $"{sqlTestsConfiguration.DbNamePrefix} Test {DateTime.Now.ToString("yyyy-MM-dd")}";

            var testInformation = TestContext.CurrentContext.Test?.GetTestInformation();
            var structuredFullName = testInformation?.StructuredFullName;
            if (structuredFullName?.Length > 0)
            {
                // Version 2
                string title = string.Join("-", structuredFullName.Reverse().Take(1).Reverse());
                string result = $"{dbPrefix}";
                if (testInformation.FormattedIndex?.Length > 0) 
                    result = $"{result} {testInformation.FormattedIndex}";
                else
                    result = $"{result} {counter:00000}";

                result = $"{result} {title}";
                result = result.Replace("\"", "");
                if (result.Length > 126) result = result.Substring(0, 126) + "…";
                return result;
            }
            else
            {
                // Version 1: + Guid
                return $"{dbPrefix} {counter:00000} {Guid.NewGuid():N}";
            }




            SqlServerTestDbManager testManager = NUnitPipelineConfiguration.GetService<SqlServerTestDbManager>();
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
