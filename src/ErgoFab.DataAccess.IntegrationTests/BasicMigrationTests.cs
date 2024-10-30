using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Universe.NUnitPipeline;

namespace ErgoFab.DataAccess.IntegrationTests;

[NUnitPipelineAction]
public class BasicMigrationTests
{
    private readonly ISqlServerTestsConfiguration TestsConfiguration = SqlServerTestsConfiguration.Instance;
    private readonly SqlServerTestDbManager _sqlTestDbManager;

    public BasicMigrationTests()
    {
        _sqlTestDbManager = new SqlServerTestDbManager(TestsConfiguration);
    }

    [Test]
    [TestCase("First")]
    [TestCase("Next")]
    public async Task TestMigration(string kind)
    {
        var testDbName = await _sqlTestDbManager.GetNextTestDatabaseName();
        await _sqlTestDbManager.CreateEmptyDatabase(testDbName);
        
        DbContextOptionsBuilder<ErgoFabDbContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<ErgoFabDbContext>();
        var connectionString = _sqlTestDbManager.BuildConnectionString(testDbName, pooling: false);
        DbConnectionString dbconnectionString = new DbConnectionString(connectionString, "TestMigration()");
        dbContextOptionsBuilder.UseSqlServer(connectionString);
        Console.WriteLine($"Test DB Connection String: {connectionString}");

        {
            ErgoFabDbContext dbContext = new ErgoFabDbContext(dbContextOptionsBuilder.Options);
            dbContext.Database.Migrate();
            var missing = dbContext.Organization.FirstOrDefault();
            Assert.IsNull(missing);
        }

        {
            ErgoFabDbContext dbContext = new ErgoFabDbContext(dbContextOptionsBuilder.Options);
            await SimpleSeeder.Seed(dbconnectionString);
            var anOrganization = dbContext.Organization.FirstOrDefault();
            Assert.NotNull(anOrganization);
        }

        // TODO: Wrap into finally{}
        await _sqlTestDbManager.DropDatabase(testDbName);
    }
}