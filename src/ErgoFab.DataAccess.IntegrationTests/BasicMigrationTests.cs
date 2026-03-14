using ErgoFab.DataAccess.IntegrationTests.Shared;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Universe.NUnitPipeline;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;
using Universe.SqlInsights.NUnit;
using Universe.SqlServerJam;

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
    [TestCase("First", 1)]
    [TestCase("Next", 333)]
    public async Task TestMigration(string kind, [BeautyParameter] int organizations)
    {
        ITestDatabaseNameProvider testDatabaseNameProvider = NUnitPipelineConfiguration.GetService<ITestDatabaseNameProvider>();
        var testDbName = await testDatabaseNameProvider.GetNextTestDatabaseName();
        var connectionString = _sqlTestDbManager.BuildConnectionString(testDbName, pooling: false);
        ResilientDbKiller.Kill(connectionString);
        await _sqlTestDbManager.CreateEmptyDatabase(testDbName);
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
            await ErgoFabDbSeeder.Seed(cs, organizations);
            var anOrganization = dbContext.Organization.FirstOrDefault();
            Assert.NotNull(anOrganization);
        }

    }
}