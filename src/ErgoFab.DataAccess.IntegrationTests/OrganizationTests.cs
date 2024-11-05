using Dapper;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Shared.TestDatabaseDefinitions;
using Universe.NUnitPipeline;
using Universe.SqlServerJam;

namespace ErgoFab.DataAccess.IntegrationTests
{
    [NUnitPipelineAction]
    [TempTestAssemblyAction]
    public class OrganizationTests
    {
        [Test]
        [ErgoFabTestCaseSource(7777)]
        public async Task OrganizationAnotherTest(ErgoFabTestCase testCase)
        {
            var organizationsCount = await testCase.CreateErgoFabDbContext().Organization.AsNoTracking().CountAsync();
            Assert.AreEqual(7777, organizationsCount);
        }


        [Test]
        [ErgoFabTestCaseSource(7777)]
        public async Task OrganizationTest(ErgoFabTestCase testCase)
        {
            Console.WriteLine(testCase.ConnectionOptions.ConnectionString);

            var connection = testCase.CreateDbConnection();
            var sqlServerVersion = connection.QueryFirst<string>("Select @@Version");
            var sqlServerMediumVersion = connection.Manage().MediumServerVersion;
            Console.WriteLine($"{Environment.NewLine}SQL Server Version: {sqlServerMediumVersion}{Environment.NewLine + sqlServerVersion}");
            
            // Assert.Fail("ON PURPOSE");
            await using ErgoFabDbContext ergoFabDbContext = testCase.CreateErgoFabDbContext();

            await using var conDbContext = ergoFabDbContext.Database.GetDbConnection();

            Console.WriteLine($"Organizations Count = [{ergoFabDbContext.Organization.Count()}]");

            var orgNames = await ergoFabDbContext.Organization
                .AsNoTracking()
                .OrderBy(x => x.Title)
                .Select(x => x.Title)
                .ToArrayAsync();


            Console.WriteLine($"ORGANIZATIONS:{Environment.NewLine}{string.Join(Environment.NewLine, orgNames).Take(44)}");
        }
    }
}
