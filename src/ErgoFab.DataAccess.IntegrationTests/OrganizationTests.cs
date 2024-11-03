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

            var conDbContext = ergoFabDbContext.Database.GetDbConnection();
            try
            {
                var mediumVersion2 = conDbContext.Manage().MediumServerVersion;
                Console.WriteLine($"MEDIUM VERSION 2: {mediumVersion2}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(@$"{Environment.NewLine}ERROR! Second Query via DBContext Connection Failed. {conDbContext.GetType()}{Environment.NewLine}{ex}");
                throw;
            }



            // Console.WriteLine($"Organizations Count = [{ergoFabDbContext.Organization.Count()}]");
        }
    }
}
