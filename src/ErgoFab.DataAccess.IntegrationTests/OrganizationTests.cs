using System.Data.SqlClient;
using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Shared.TestDatabaseDefinitions;
using Universe.NUnitPipeline;

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
            // Assert.Fail("ON PURPOSE");
            ErgoFabDbContext ergoFabDbContext = testCase.CreateErgoFabDbContext();

            try
            {
                await ergoFabDbContext.Database.MigrateAsync();
                await SimpleSeeder.Seed(testCase.ConnectionOptions);
            }
            finally
            {
                ergoFabDbContext.Database.EnsureDeleted();
            }
        }
    }
}
