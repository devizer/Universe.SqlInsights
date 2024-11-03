using System.Data.SqlClient;
using ErgoFab.DataAccess.IntegrationTests.Library;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Shared.TestDatabaseDefinitions;
using Universe.NUnitPipeline;
using Universe.SqlServerJam;

namespace ErgoFab.DataAccess.IntegrationTests;

[NUnitPipelineAction]
public class BackupRestoreTests
{
    [Test]
    [EmptyDbTestCaseSource]
    public async Task OrganizationTest(ErgoFabTestCase testCase)
    {
        Console.WriteLine(testCase.ConnectionOptions.ConnectionString);

        SqlServerTestDbManager man = new SqlServerTestDbManager(SqlServerTestsConfiguration.Instance);
        await man.CreateEmptyDatabase(testCase.ConnectionOptions);
        
        var ergoFabDbContext = testCase.CreateErgoFabDbContext();

        try
        {
            ergoFabDbContext.Database.Migrate();

            ergoFabDbContext.Organization.Add(new Organization() { Title = "Azure Dev-Ops" });
            ergoFabDbContext.SaveChanges();

            var csb = man.CreateDbProviderFactory().CreateConnectionStringBuilder();
            csb.ConnectionString = testCase.ConnectionOptions.ConnectionString;
            // var dbName = new System.Data.SqlClient.SqlConnectionStringBuilder(testCase.ConnectionOptions.ConnectionString).InitialCatalog;
            var dbName = man.GetDatabaseName(testCase.ConnectionOptions.ConnectionString);

            var backup = await man.CreateBackup($"ErgoFab-777-{Guid.NewGuid():N}", dbName);

            var dbRestoredName = $"ErgoFab-777-Restored-{Guid.NewGuid():N}";
            await man.RestoreBackup(backup, dbRestoredName);

            var isDbExists = (await man.GetDatabaseNames()).Contains(dbRestoredName);
            Assert.True(isDbExists, $"Missing Restored DB {dbRestoredName}");
            
            Assert.IsTrue(File.Exists(backup.BackupName), $"Missing Backup file {backup.BackupName}");
            TryAndForget.Execute(() => File.Delete(backup.BackupName));

            TestDbConnectionString newCs = new TestDbConnectionString(man.BuildConnectionString(dbRestoredName), "Restored");
            var newDbContext = newCs.CreateErgoFabDbContext();
            var newOrg = newDbContext.Organization.FirstOrDefault(x => x.Title == "Azure Dev-Ops");
            Assert.IsNotNull(newOrg, "Missing 'Azure Dev-Ops' organization on restored DB");

            // newDbContext.Database.EnsureDeleted();
            AgileDbKiller.Kill(newCs.ConnectionString);
        }
        finally
        {
            // await ergoFabDbContext.Database.EnsureDeletedAsync();
            AgileDbKiller.Kill(testCase.ConnectionOptions.ConnectionString);
        }
    }
}