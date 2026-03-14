using ErgoFab.DataAccess.IntegrationTests.Shared;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;

namespace Shared.TestDatabaseDefinitions;

// DB With migrations and 0...organizationsCount Organizations
public class ErgoFabTestCaseSource : TestCaseSourceAttribute
{
    public ErgoFabTestCaseSource() : this(42)
    {
    }

    public ErgoFabTestCaseSource(int organizationsCount) : base(typeof(ErgoFabTestCaseSource), nameof(GetTestCases), new object[] { organizationsCount })
    {
    }

    public static IEnumerable<ErgoFabTestCase> GetTestCases(int organizationsCount)
    {
        var dbDefinition = new ErgoFabDatabase(organizationsCount);

        // TestContext.CurrentContext.Test.GetPropertyOrAdd<IDatabaseDefinition>("DatabaseDefinition", test => dbDefinition);

        var humanIndexes = new List<string> { "First", "Next" };
        for(int i = 0; i < ErgoFabEnvironment.AdditionalTestCount; i++)
        {
            int index = i + 3;
            if (index == 3) humanIndexes.Add("Third");
            else if (index == 4) humanIndexes.Add("Fourth");
            else humanIndexes.Add($"{index}th");
        }

        foreach (var kind in humanIndexes)
        {
            yield return new ErgoFabTestCase()
            {
                ConnectionOptions = TestDbConnectionString.CreatePostponed(dbDefinition),
                Kind = kind,
            };
        }
    }



}