using ErgoFab.DataAccess.IntegrationTests.Library;
using Universe.NUnitPipeline;

namespace Library;

public class DbTestConfiguration
{
    static DbTestConfiguration()
    {
    }

    public static ISqlServerTestsConfiguration SqlServerTestsConfiguration => NUnitPipelineConfiguration.GetService<ISqlServerTestsConfiguration>();
}