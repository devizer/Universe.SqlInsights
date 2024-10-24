using System.Data.SqlClient;
using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;

namespace ErgoFab.DataAccess.IntegrationTests
{
    public class OrganizationTests
    {
        [Test]
        [ErgoFabEmptyTestCaseSource]
        public async Task OrganizationTest(ErgoFabTestCase testCase)
        {
            Console.WriteLine(testCase.ConnectionOptions.ConnectionString);
            var ergoFabDbContext = testCase.ConnectionOptions.CreateErgoFabDbContext();
            // Database is Missing

            try
            {
                ergoFabDbContext.Database.Migrate();
            }
            finally
            {
                ergoFabDbContext.Database.EnsureDeleted();
            }
        }
    }
}
