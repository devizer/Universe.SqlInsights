using Microsoft.EntityFrameworkCore;
using Universe.SqlInsights.NUnit;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

// TODO: Throw up
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

        return await base.BuildDatabase(cacheKey, newDbName, title, MigrateAndSeed);
    }

}