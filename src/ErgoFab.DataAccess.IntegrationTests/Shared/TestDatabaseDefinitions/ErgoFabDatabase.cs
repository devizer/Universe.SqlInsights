using ErgoFab.DataAccess.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using Universe.SqlInsights.NUnit;

namespace Shared.TestDatabaseDefinitions;

public class ErgoFabDatabase : IDatabaseDefinition
{
    public int OrganizationsCount { get; }

    public ErgoFabDatabase(int organizationsCount)
    {
        OrganizationsCount = organizationsCount;
    }

    public string Title => $"Ergo Fab DB {(OrganizationsCount == 0 ? "without Organizations" : OrganizationsCount == 1 ? "with 1 TheOrganization" : $"with {OrganizationsCount} organizations")}";
    public string CacheKey => Title;
    public void MigrateAndSeed(IDbConnectionString connectionOptions)
    {
        using var dbContext = connectionOptions.CreateErgoFabDbContext();
        dbContext.Database.MigrateAsync().SafeWait();
        ErgoFabDbSeeder.Seed(connectionOptions, OrganizationsCount).SafeWait();
    }
}
