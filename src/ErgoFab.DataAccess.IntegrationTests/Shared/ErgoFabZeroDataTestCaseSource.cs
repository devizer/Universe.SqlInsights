using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Universe.NUnitPipeline;
using Universe.SqlInsights.NUnit;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

public class ErgoFabZeroDataTestCaseSource : TestCaseSourceAttribute
{
    public static IEnumerable<ErgoFabTestCase> GetTestCases()
    {
        foreach (var kind in new[] { "First", "Next" })
        {
            SqlServerTestDbManager man = new SqlServerTestDbManager(SqlServerTestsConfiguration.Instance);
            var testDatabaseNameProvider = NUnitPipelineConfiguration.GetService<ITestDatabaseNameProvider>();
            var testDbName = testDatabaseNameProvider.GetNextTestDatabaseName().GetSafeResult();
            TestCleaner.OnDispose($"Drop DB '{testDbName}'", () => man.DropDatabase(testDbName).SafeWait(), TestDisposeOptions.AsyncGlobal);

            string connectionString = man.BuildConnectionString(testDbName);

            // Ensure Data and Log files are created on specified folder
            man.CreateEmptyDatabase(testDbName).SafeWait();

            var connectionOptions = new TestDbConnectionString(connectionString, "Empty DB with Migrations");
            using (ErgoFabDbContext ergoFabDbContext = connectionOptions.CreateErgoFabDbContext())
            {
                ergoFabDbContext.Database.Migrate();
            }

            yield return new ErgoFabTestCase()
            {
                ConnectionOptions = connectionOptions,
                Kind = kind,
            };
        }
    }

    public ErgoFabZeroDataTestCaseSource() : base(typeof(ErgoFabZeroDataTestCaseSource), nameof(GetTestCases))
    {
    }

}