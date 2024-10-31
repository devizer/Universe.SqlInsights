using ErgoFab.DataAccess.IntegrationTests.Library;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

public class ErgoFabSimpleSeededTestCaseSource : TestCaseSourceAttribute
{
    public static IEnumerable<ErgoFabTestCase> GetTests()
    {
        foreach (var kind in new[] { "First", "Next" })
        {
            SqlServerTestDbManager man = new SqlServerTestDbManager(SqlServerTestsConfiguration.Instance);
            var testDbName = man.GetNextTestDatabaseName().Result;
            var cs = man.BuildConnectionString(testDbName);


            yield return new ErgoFabTestCase()
            {
                ConnectionOptions = new DbConnectionString(cs, "Empty DB"),
                Kind = kind,
            };
        }
    }

    public ErgoFabSimpleSeededTestCaseSource() : base(typeof(ErgoFabSimpleSeededTestCaseSource), nameof(GetTests))
    {
    }

}