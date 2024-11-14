using Universe.NUnitPipeline;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;

namespace Universe.SqlInsights.NUnit;

public class DbTestConfiguration
{
    static DbTestConfiguration()
    {
    }

    public static ISqlServerTestsConfiguration SqlServerTestsConfiguration => NUnitPipelineConfiguration.GetService<ISqlServerTestsConfiguration>();
}