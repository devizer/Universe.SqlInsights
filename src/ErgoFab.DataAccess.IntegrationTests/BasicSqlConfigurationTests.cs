using Dapper;
using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using Universe.SqlServerJam;

namespace ErgoFab.DataAccess.IntegrationTests;

public class BasicSqlConfigurationTests
{
    private readonly ISqlServerTestsConfiguration TestsConfiguration = SqlServerTestsConfiguration.Instance;
    private readonly SqlServerTestsManager SqlTestsManager;

    public BasicSqlConfigurationTests()
    {
        SqlTestsManager = new SqlServerTestsManager(TestsConfiguration);
    }


    [Test]
    [TestCase("First")]
    [TestCase("Next")]
    public async Task TestMasterConnection(string kind)
    {
        var version = SqlTestsManager.CreateMasterConnection().Manage().MediumServerVersion;
        Console.WriteLine($"SQL Server Version: {version}");

        var dbList = await SqlTestsManager.GetDatabaseNames();
        Console.WriteLine($"Total DB Count: {dbList.Length}");
    }

    [Test]
    [TestCase("First")]
    [TestCase("Next")]
    public async Task TryNextTestDatabaseName(string kind)
    {
        var nextTestDbName = await SqlTestsManager.GetNextTestDatabaseName();
        Console.WriteLine($"Next test database name is {nextTestDbName}");
    }

    [Test]
    [TestCase("First")]
    [TestCase("Next")]
    public async Task TestCreateDb(string kind)
    {
        var testDbName = await SqlTestsManager.GetNextTestDatabaseName();
        await SqlTestsManager.CreateEmptyDatabase(testDbName);
        var dbList = await SqlTestsManager.GetDatabaseNames();
        bool isContains = dbList.Contains(testDbName);
        await SqlTestsManager.CreateMasterConnection().ExecuteAsync($"Drop Database [{testDbName}]"); ;

        // Assert 1
        Assert.True(isContains);

        // Assert 2
        var dbListAfter = await SqlTestsManager.GetDatabaseNames();
        Assert.False(dbListAfter.Contains(testDbName));
    }
}