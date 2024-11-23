using ErgoFab.DataAccess.IntegrationTests.Shared;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Shared.TestDatabaseDefinitions;
using Universe.NUnitPipeline;
using Universe.NUnitPipeline.SqlServerDatabaseFactory;
using Universe.SqlInsights.NUnit;
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

        // Add Org before backup
        await using (var ergoFabDbContext = testCase.CreateErgoFabDbContext())
        {
            await ergoFabDbContext.Database.MigrateAsync();
            ergoFabDbContext.Organization.Add(new Organization() { Title = "Azure Dev-Ops" });
            await ergoFabDbContext.SaveChangesAsync();
        }

        var csb = man.CreateDbProviderFactory().CreateConnectionStringBuilder();
        csb.ConnectionString = testCase.ConnectionOptions.ConnectionString;
        // var dbName = new System.Data.SqlClient.SqlConnectionStringBuilder(testCase.ConnectionOptions.ConnectionString).InitialCatalog;
        var dbName = man.GetDatabaseName(testCase.ConnectionOptions.ConnectionString);

        // 1. Create Explicit Backup
        SqlBackupDescription? backup = await man.CreateBackup($"ErgoFab-777-{Guid.NewGuid():N}", dbName);
        TestCleaner.OnDispose($"Drop Explicit Backup {backup.BackupPoint}", () => File.Delete(backup.BackupPoint));

        var dbRestoredName = $"Ergo Fab Test Restored Explicitly {Guid.NewGuid():N}";
        TestCleaner.OnDispose($"Drop Restored DB {dbRestoredName}", () => man.DropDatabase(dbRestoredName).SafeWait());

        // 2. Restore Backup
        await man.RestoreBackup(backup, dbRestoredName);

        var isDbExists = (await man.GetDatabaseNames()).Contains(dbRestoredName);
        Assert.True(isDbExists, $"Missing Restored DB {dbRestoredName}");

        // TODO: Assert is Only for local SQL Server
        Assert.IsTrue(File.Exists(backup.BackupPoint), $"Missing Backup file {backup.BackupPoint}");

        // 3. Query newly restored DB
        TestDbConnectionString newCs = new TestDbConnectionString(man.BuildConnectionString(dbRestoredName), "Restored");
        await using var newDbContext = newCs.CreateErgoFabDbContext();
        var orgCount = newDbContext.Organization.AsNoTracking().Count();
        Console.WriteLine($"Organizations Count {orgCount}");
#if !NET6_0 && !NET7_0
        var newOrg = newDbContext.Organization.AsNoTracking().FirstOrDefault(x => x.Title == "Azure Dev-Ops");
        Assert.IsNotNull(newOrg, "Missing 'Azure Dev-Ops' organization on restored DB");
#endif
    }
}