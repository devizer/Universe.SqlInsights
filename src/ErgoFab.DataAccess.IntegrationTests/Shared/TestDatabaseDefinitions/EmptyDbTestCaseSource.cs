using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using Library;
using Universe.NUnitPipeline;

namespace Shared.TestDatabaseDefinitions;

// Empty DB
public class EmptyDbTestCaseSource : TestCaseSourceAttribute
{
    public static IEnumerable<ErgoFabTestCase> GetTestCases()
    {
        // TestContext.CurrentContext.Test.GetPropertyOrAdd<IDatabaseDefinition>("DatabaseDefinition", test => EmptyDatabase.Instance);

        foreach (var kind in new[] { "First", "Next" })
        {
            yield return new ErgoFabTestCase()
            {
                ConnectionOptions = TestDbConnectionString.CreatePostponed(EmptyDatabase.Instance),
                Kind = kind,
            };
        }
    }

    public EmptyDbTestCaseSource() : base(typeof(EmptyDbTestCaseSource), nameof(GetTestCases))
    {
    }
}