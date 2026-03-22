using Microsoft.EntityFrameworkCore;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

// TODO: REMOVE IT
[Obsolete("SeededDatabaseFactory is enough" ,true)]
public class SeededEfDatabaseFactory : SeededDatabaseFactory
{
    public SeededEfDatabaseFactory(ISqlServerTestsConfiguration sqlServerTestsConfiguration) : base(sqlServerTestsConfiguration)
    {
    }

    // TODO 1: MOVE TO Shared/ as Extension
    public async Task<IDbConnectionString> BuildEfDatabase<TDbContext>(string cacheKey, string newDbName, string title, string playgroundDatabaseName, Func<IDbConnectionString, TDbContext> createDbContext, Func<IDbConnectionString,Task> actionSeed) where TDbContext : DbContext
    {
        Task MigrateEfAndSeed(IDbConnectionString dbConnection)
        {
            using (var dbContext = createDbContext(dbConnection))
            {
                dbContext.Database.Migrate();
            }
            using (var dbContext = createDbContext(dbConnection))
            {
                actionSeed(dbConnection);
            }

			return Task.CompletedTask;
        }

        // TODO: Saved DB Name
        return await base.BuildDatabase(cacheKey, newDbName, title, playgroundDatabaseName, MigrateEfAndSeed);
    }

}