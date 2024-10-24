using ErgoFab.DataAccess.IntegrationTests.Library;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

public class ErgoFabEmptyTestCaseSource : TestCaseSourceAttribute
{
    public static IEnumerable<ErgoFabTestCase> GetTestCases()
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

    public ErgoFabEmptyTestCaseSource() : base(typeof(ErgoFabEmptyTestCaseSource), nameof(GetTestCases))
    {
    }

    public ErgoFabEmptyTestCaseSource(string sourceName) : base(sourceName)
    {
    }

    public ErgoFabEmptyTestCaseSource(Type sourceType, string sourceName, object?[]? methodParams) : base(sourceType, sourceName, methodParams)
    {
    }

    public ErgoFabEmptyTestCaseSource(Type sourceType, string sourceName) : base(sourceType, sourceName)
    {
    }

    public ErgoFabEmptyTestCaseSource(string sourceName, object?[]? methodParams) : base(sourceName, methodParams)
    {
    }

    public ErgoFabEmptyTestCaseSource(Type sourceType) : base(sourceType)
    {
    }
}