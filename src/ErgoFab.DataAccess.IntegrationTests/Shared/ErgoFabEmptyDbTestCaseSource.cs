using ErgoFab.DataAccess.IntegrationTests.Library;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

public class ErgoFabEmptyDbTestCaseSource : TestCaseSourceAttribute
{
    public static IEnumerable<ErgoFabTestCase> GetTestCases()
    {
        foreach (var kind in new[] { "First", "Next" })
        {
            SqlServerTestDbManager man = new SqlServerTestDbManager(SqlServerTestsConfiguration.Instance);
            var testDbName = man.GetNextTestDatabaseName().Result;
            string cs = man.BuildConnectionString(testDbName);
            // Ensure Data and Log files are created on specified folder
            man.CreateEmptyDatabase(testDbName).Wait();

            yield return new ErgoFabTestCase()
            {
                ConnectionOptions = new DbConnectionString(cs, "Empty DB without Migrations"),
                Kind = kind,
            };
        }
    }

    public ErgoFabEmptyDbTestCaseSource() : base(typeof(ErgoFabEmptyDbTestCaseSource), nameof(GetTestCases))
    {
    }

}