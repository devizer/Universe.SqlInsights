using ErgoFab.DataAccess.IntegrationTests.Shared;
using Universe.NUnitPipeline;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;
using Universe.SqlInsights.NUnit;

namespace Shared.TestDatabaseDefinitions;

// Empty DB
public class EmptyDbTestCaseSource : TestCaseSourceAttribute
{
    public static IEnumerable<TestCaseData> GetTestCases()
    {
        // TestContext.CurrentContext.Test.GetPropertyOrAdd<IDatabaseDefinition>("DatabaseDefinition", test => EmptyDatabase.Instance);

        foreach (var kind in new[] { "First", "Next" })
        {
            var ergoFabTestCase = new ErgoFabTestCase()
            {
                ConnectionOptions = TestDbConnectionString.CreatePostponed(EmptyDatabase.Instance),
                Kind = kind,
            };
            TestCaseData tcd = new TestCaseData(ergoFabTestCase).SetArgDisplayNames($"My {ergoFabTestCase}");
            yield return tcd;
        }
    }

    public EmptyDbTestCaseSource() : base(typeof(EmptyDbTestCaseSource), nameof(GetTestCases))
    {
    }
}