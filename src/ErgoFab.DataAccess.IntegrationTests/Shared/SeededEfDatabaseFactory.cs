using Microsoft.EntityFrameworkCore;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

// TODO: REMOVE IT
public class SeededEfDatabaseFactory : SeededDatabaseFactory
{
    public SeededEfDatabaseFactory(ISqlServerTestsConfiguration sqlServerTestsConfiguration) : base(sqlServerTestsConfiguration)
    {
    }

    // TODO 1: MOVE TO Shared/ as Extension
    public async Task<IDbConnectionString> BuildEfDatabase<TDbContext>(string cacheKey, string newDbName, string title, Func<IDbConnectionString, TDbContext> createDbContext, Action<IDbConnectionString> actionSeed) where TDbContext : DbContext
    {
        void MigrateAndSeed(IDbConnectionString dbConnection)
        {
            using (var dbContext = createDbContext(dbConnection))
            {
                dbContext.Database.Migrate();
            }
            using (var dbContext = createDbContext(dbConnection))
            {
                actionSeed(dbConnection);
            }
        }

        // TODO: Saved DB Name
        return await base.BuildDatabase(cacheKey, newDbName, title, null, MigrateAndSeed);
    }

}