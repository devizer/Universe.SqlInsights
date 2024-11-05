﻿namespace Universe.SqlInsights.NUnit
{
    public interface IDatabaseDefinition
    {
        string Title { get; }
        
        // 2nd+ test cases restore database from cache
        // Null means do not cache
        string CacheKey { get; }
        
        // Test DB Is Created. Initial Catalog is properly assigned
        void MigrateAndSeed(IDbConnectionString connectionOptions);
    }
}
