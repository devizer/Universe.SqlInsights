using System.Data.SqlClient;
using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Universe.NUnitPipeline;

namespace ErgoFab.DataAccess.IntegrationTests
{
    [NUnitPipelineAction]
    public class OrganizationTests
    {
        [Test]
        [ErgoFabEmptyDbTestCaseSource]
        public async Task AncientOrganizationTest(ErgoFabTestCase testCase)
        {
            Console.WriteLine(testCase.ConnectionOptions.ConnectionString);
            ErgoFabDbContext ergoFabDbContext = testCase.ConnectionOptions.CreateErgoFabDbContext();

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
