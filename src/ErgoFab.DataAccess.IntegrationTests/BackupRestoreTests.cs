﻿using ErgoFab.DataAccess.IntegrationTests.Shared;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Shared.TestDatabaseDefinitions;
using Universe.NUnitPipeline;
using Universe.SqlInsights.NUnit;

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

        ergoFabDbContext.Database.Migrate();

        ergoFabDbContext.Organization.Add(new Organization() { Title = "Azure Dev-Ops" });
        ergoFabDbContext.SaveChanges();

        var csb = man.CreateDbProviderFactory().CreateConnectionStringBuilder();
        csb.ConnectionString = testCase.ConnectionOptions.ConnectionString;
        // var dbName = new System.Data.SqlClient.SqlConnectionStringBuilder(testCase.ConnectionOptions.ConnectionString).InitialCatalog;
        var dbName = man.GetDatabaseName(testCase.ConnectionOptions.ConnectionString);

        // 1. Create Explicit Backup
        var backup = await man.CreateBackup($"ErgoFab-777-{Guid.NewGuid():N}", dbName);
        TestCleaner.OnDispose($"Drop Explicit Backup {backup.BackupName}", () => File.Delete(backup.BackupName));

        var dbRestoredName = $"ErgoFab Restored Explicitly {Guid.NewGuid():N}";
        TestCleaner.OnDispose($"Drop Restored DB {dbRestoredName}", () => man.DropDatabase(dbRestoredName).SafeWait());

        // 2. Restore Backup
        await man.RestoreBackup(backup, dbRestoredName);

        var isDbExists = (await man.GetDatabaseNames()).Contains(dbRestoredName);
        Assert.True(isDbExists, $"Missing Restored DB {dbRestoredName}");

        // TODO: Assert is Only for local SQL Server
        Assert.IsTrue(File.Exists(backup.BackupName), $"Missing Backup file {backup.BackupName}");

        // 3. Query newly restored DB
        TestDbConnectionString newCs = new TestDbConnectionString(man.BuildConnectionString(dbRestoredName), "Restored");
        var newDbContext = newCs.CreateErgoFabDbContext();
        var newOrg = newDbContext.Organization.FirstOrDefault(x => x.Title == "Azure Dev-Ops");
        Assert.IsNotNull(newOrg, "Missing 'Azure Dev-Ops' organization on restored DB");
    }
}