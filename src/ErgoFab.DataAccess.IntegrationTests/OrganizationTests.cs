using System.Data.SqlClient;
using Dapper;
using ErgoFab.DataAccess.IntegrationTests.Library;
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
        [ErgoFabTestCaseSource(7)]
        public async Task AncientOrganizationTest(ErgoFabTestCase testCase)
        {
            Console.WriteLine(testCase.ConnectionOptions.ConnectionString);

            var connection = testCase.CreateDbConnection();
            var sqlServerVersion = connection.QueryFirst<string>("Select @@Version");
            var sqlServerMediumVersion = connection.Manage().MediumServerVersion;
            Console.WriteLine($"{Environment.NewLine}SQL Server Version: {sqlServerMediumVersion}{Environment.NewLine + sqlServerVersion}");

            // Assert.Fail("ON PURPOSE");
            using ErgoFabDbContext ergoFabDbContext = testCase.CreateErgoFabDbContext();


            // Console.WriteLine($"Organizations Count = [{ergoFabDbContext.Organization.Count()}]");
        }
    }
}
