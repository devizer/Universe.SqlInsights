using Dapper;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
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
        public async Task OrganizationAnotherTest1st(ErgoFabTestCase testCase)
        {
            var organizationsCount = await testCase.CreateErgoFabDbContext().Organization.AsNoTracking().CountAsync();
            Assert.AreEqual(7777, organizationsCount);
            var organizationsCount2 = await testCase.CreateErgoFabDbContext().Organization.AsNoTracking().CountAsync();
        }

        [Test]
        [ErgoFabTestCaseSource(7777)]
        public async Task OrganizationAnotherTest2nd(ErgoFabTestCase testCase)
        {
            var organizationsCount = await testCase.CreateErgoFabDbContext().Organization.AsNoTracking().Select(x => x.Id).CountAsync();
            Assert.AreEqual(7777, organizationsCount);
            var organizationsCount2 = await testCase.CreateErgoFabDbContext().Organization.AsNoTracking().Select(x => x.Id).CountAsync();
        }

        [Test]
        [ErgoFabTestCaseSource(7777)]
        public async Task Organization3rdTest(ErgoFabTestCase testCase)
        {
            Console.WriteLine($"CS: [{testCase.ConnectionOptions.ConnectionString}]");
            using var db = testCase.CreateErgoFabDbContext();
            var organizations = await db.Organization.AsNoTracking()
                .Include(o => o.TheDirector)
                .Include(o => o.TheDepartments)
                .ToListAsync();

            Assert.AreEqual(7777, organizations.Count);
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

            // Test for missing
            var missingOrg = ergoFabDbContext.Organization.FirstOrDefault(x => x.Title == Guid.NewGuid().ToString());
            
            using var db2 = testCase.CreateErgoFabDbContext();
            var existingOrg = db2.Organization.FirstOrDefault(x => x.Title == orgNames.First());



            Console.WriteLine($"ORGANIZATIONS:{Environment.NewLine}{string.Join(Environment.NewLine, orgNames).Take(44)}");
        }
    }
}
