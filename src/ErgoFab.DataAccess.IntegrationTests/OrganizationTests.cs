using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using Microsoft.EntityFrameworkCore;

namespace ErgoFab.DataAccess.IntegrationTests
{
    public class OrganizationTests
    {
        [Test]
        [ErgoFabEmptyTestCaseSource]
        public void OrganizationTest(ErgoFabTestCase testCase)
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
