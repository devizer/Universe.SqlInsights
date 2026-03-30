using ErgoFab.DataAccess.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;

namespace Shared.TestDatabaseDefinitions;

public class ErgoFabDatabase(int organizationsCount) : IDatabaseDefinition
{
	public string Title => organizationsCount switch
	{
		0 => "Ergo Fab DB without rows",
		1 => "Ergo Fab DB with 1 row",
		var n => $"Ergo Fab DB with {n.ToString("n0").Replace(",","_")} rows"
	};

	public string CacheKey => ErgoFabEnvironment.IgnoreCache ? null : Title;
	public async Task MigrateAndSeed(IDbConnectionString connectionOptions)
	{
		await using var dbContext = connectionOptions.CreateErgoFabDbContext();
		await dbContext.Database.MigrateAsync();
		await ErgoFabDbSeeder.Seed(connectionOptions, organizationsCount);
	}

	public string PlaygroundDatabaseName => Title;
}