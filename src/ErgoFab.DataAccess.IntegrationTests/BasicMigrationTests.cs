using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;

namespace ErgoFab.DataAccess.IntegrationTests;

public class BasicMigrationTests
{
    private readonly ISqlServerTestsConfiguration TestsConfiguration = SqlServerTestsConfiguration.Instance;
    private readonly SqlServerTestsManager SqlTestsManager;

    public BasicMigrationTests()
    {
        SqlTestsManager = new SqlServerTestsManager(TestsConfiguration);
    }

    [Test]
    [TestCase("First")]
    [TestCase("Next")]
    public async Task TestMigration(string kind)
    {
        var testDbName = await SqlTestsManager.GetNextTestDatabaseName();
        await SqlTestsManager.CreateEmptyDatabase(testDbName);
        
        DbContextOptionsBuilder<ErgoFabDbContext> dbContextOptionsBuilder = new DbContextOptionsBuilder<ErgoFabDbContext>();
        var connectionString = SqlTestsManager.BuildConnectionString(testDbName, pooling: false);
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
        await SqlTestsManager.DropDatabase(testDbName);
    }
}