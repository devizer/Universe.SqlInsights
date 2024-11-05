using ErgoFab.DataAccess.IntegrationTests.Library;
using Universe.NUnitPipeline;
using Universe.SqlInsights.NUnit;

namespace Library;

public class DbTestConfiguration
{
    static DbTestConfiguration()
    {
    }

    public static ISqlServerTestsConfiguration SqlServerTestsConfiguration => NUnitPipelineConfiguration.GetService<ISqlServerTestsConfiguration>();
}