using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using Library;
using Universe.NUnitPipeline;

namespace Shared.TestDatabaseDefinitions;

// DB With migrations and 0...organizationsCount Organizations
public class ErgoFabTestCaseSource : TestCaseSourceAttribute
{
    public ErgoFabTestCaseSource(int organizationsCount) : base(typeof(ErgoFabTestCaseSource), nameof(GetTestCases), new object[] { organizationsCount })
    {
    }

    public static IEnumerable<ErgoFabTestCase> GetTestCases(int organizationsCount)
    {
        var dbDefinition = new ErgoFabDatabase(organizationsCount);

        // TestContext.CurrentContext.Test.GetPropertyOrAdd<IDatabaseDefinition>("DatabaseDefinition", test => dbDefinition);
        foreach (var kind in new[] { "First", "Next" })
        {
            yield return new ErgoFabTestCase()
            {
                ConnectionOptions = TestDbConnectionString.CreatePostponed(dbDefinition),
                Kind = kind,
            };
        }
    }


}