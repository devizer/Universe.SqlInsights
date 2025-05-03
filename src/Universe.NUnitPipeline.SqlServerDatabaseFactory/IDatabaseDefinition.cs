namespace Universe.NUnitPipeline.SqlServerDatabaseFactory
{
    public interface IDatabaseDefinition
    {
        string Title { get; }
        
        // 2nd+ test cases restore database from cache
        // Null means do not cache
        string CacheKey { get; }
        
        // Test DB Is Created. Initial Catalog is properly assigned
        void MigrateAndSeed(IDbConnectionString connectionOptions);

        // Nullable. If not null then infrastructure keep DB with the name
        // This database is always reset on first test case invocation
        string PlaygroundDatabaseName { get; }
    }
}
