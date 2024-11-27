namespace Universe.NUnitPipeline.SqlServerDatabaseFactory;

public class DbTestConfiguration
{
    static DbTestConfiguration()
    {
    }

    // TODO: Remove or add all the dependencies
    public static ISqlServerTestsConfiguration SqlServerTestsConfiguration => NUnitPipelineConfiguration.GetService<ISqlServerTestsConfiguration>();
}
