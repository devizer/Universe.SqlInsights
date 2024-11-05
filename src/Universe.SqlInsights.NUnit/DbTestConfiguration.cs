using Universe.NUnitPipeline;

namespace Universe.SqlInsights.NUnit;

public class DbTestConfiguration
{
    static DbTestConfiguration()
    {
    }

    public static ISqlServerTestsConfiguration SqlServerTestsConfiguration => NUnitPipelineConfiguration.GetService<ISqlServerTestsConfiguration>();
}