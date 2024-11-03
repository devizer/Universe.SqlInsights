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
    [TestCase("First", 7777)]
    [TestCase("Next", 7777)]
    public async Task TestMigration(string kind, [BeautyParameter] int organizations)
    {
        var testDbName = await _sqlTestDbManager.GetNextTestDatabaseName();
        await _sqlTestDbManager.CreateEmptyDatabase(testDbName);
        var connectionString = _sqlTestDbManager.BuildConnectionString(testDbName, pooling: false);
        TestCleaner.OnDispose(
            $"Drop DB '{testDbName}'",
            () => _sqlTestDbManager.DropDatabase(testDbName).SafeWait()
        );

        TestDbConnectionString cs = new TestDbConnectionString(connectionString, "Test Migrations");
        Console.WriteLine($"Test DB Connection String: {connectionString}");

        {

            await using var dbContext = cs.CreateErgoFabDbContext();
            dbContext.Database.Migrate();
            var missing = dbContext.Organization.FirstOrDefault();
            Assert.IsNull(missing);
        }

        {
            await using var dbContext = cs.CreateErgoFabDbContext();
            await SimpleSeeder.Seed(cs, organizations);
            var anOrganization = dbContext.Organization.FirstOrDefault();
            Assert.NotNull(anOrganization);
        }

    }
}