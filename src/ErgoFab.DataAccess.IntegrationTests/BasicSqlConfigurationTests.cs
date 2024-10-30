using Dapper;
using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using Universe.NUnitPipeline;
using Universe.SqlServerJam;

namespace ErgoFab.DataAccess.IntegrationTests;

[NUnitPipelineAction]
public class BasicSqlConfigurationTests
{
    private readonly ISqlServerTestsConfiguration TestsConfiguration = SqlServerTestsConfiguration.Instance;
    private readonly SqlServerTestDbManager _sqlTestDbManager;

    public BasicSqlConfigurationTests()
    {
        _sqlTestDbManager = new SqlServerTestDbManager(TestsConfiguration);
    }


    [Test]
    [TestCase("First")]
    [TestCase("Next")]
    public async Task TestMasterConnection(string kind)
    {
        var version = _sqlTestDbManager.CreateMasterConnection().Manage().MediumServerVersion;
        Console.WriteLine($"SQL Server Version: {version}");

        var dbList = await _sqlTestDbManager.GetDatabaseNames();
        Console.WriteLine($"Total DB Count: {dbList.Length}");
    }

    [Test]
    [TestCase("First")]
    [TestCase("Next")]
    public async Task TryNextTestDatabaseName(string kind)
    {
        var nextTestDbName = await _sqlTestDbManager.GetNextTestDatabaseName();
        Console.WriteLine($"Next test database name is {nextTestDbName}");
    }

    [Test]
    [TestCase("First")]
    [TestCase("Next")]
    public async Task TestCreateDb(string kind)
    {
        var testDbName = await _sqlTestDbManager.GetNextTestDatabaseName();
        Console.WriteLine($"Test DB Name: {testDbName}");
        await _sqlTestDbManager.CreateEmptyDatabase(testDbName);
        var dbList = await _sqlTestDbManager.GetDatabaseNames();
        bool isContains = dbList.Contains(testDbName);
        await _sqlTestDbManager.CreateMasterConnection().ExecuteAsync($"Drop Database [{testDbName}]"); ;

        // Assert 1
        Assert.True(isContains);

        // Assert 2
        var dbListAfter = await _sqlTestDbManager.GetDatabaseNames();
        Assert.False(dbListAfter.Contains(testDbName));
    }
}