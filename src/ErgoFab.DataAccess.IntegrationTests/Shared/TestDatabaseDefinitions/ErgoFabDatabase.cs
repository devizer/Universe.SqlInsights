using ErgoFab.DataAccess.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;
using Universe.SqlInsights.NUnit;

namespace Shared.TestDatabaseDefinitions;

public class ErgoFabDatabase : IDatabaseDefinition
{
    public int OrganizationsCount { get; }

    public ErgoFabDatabase(int organizationsCount)
    {
        OrganizationsCount = organizationsCount;
    }

    public string Title => $"Ergo Fab DB {(OrganizationsCount == 0 ? "without rows" : OrganizationsCount == 1 ? "with 1 rows" : $"with {OrganizationsCount.ToString("n0").Replace(",", "_")} rows")}";
    public string CacheKey => ErgoFabEnvironment.IgnoreCache ? null : Title;
    public void MigrateAndSeed(IDbConnectionString connectionOptions)
    {
        using var dbContext = connectionOptions.CreateErgoFabDbContext();
        dbContext.Database.MigrateAsync().SafeWait();
        ErgoFabDbSeeder.Seed(connectionOptions, OrganizationsCount).SafeWait();
    }

    public string PlaygroundDatabaseName => Title;

}
