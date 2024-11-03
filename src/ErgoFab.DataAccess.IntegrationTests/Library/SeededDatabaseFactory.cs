using System.Collections.Concurrent;
using System.Diagnostics;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using Universe.NUnitPipeline;

namespace ErgoFab.DataAccess.IntegrationTests.Library
{
    public class SeededDatabaseFactory
    {
        public readonly ISqlServerTestsConfiguration SqlServerTestsConfiguration;

        private static readonly ConcurrentDictionary<string, DatabaseBackupInfo> Cache = new();


        public SeededDatabaseFactory(ISqlServerTestsConfiguration sqlServerTestsConfiguration)
        {
            SqlServerTestsConfiguration = sqlServerTestsConfiguration;
        }


        // TODO 2: Bind to OrganizationTests
        public async Task<IDbConnectionString> BuildDatabase(string cacheKey, string newDbName, string title, Action<IDbConnectionString> actionMigrateAndSeed)
        {
            // new DB Name already asigned
            SqlServerTestDbManager sqlServerTestDbManager = new SqlServerTestDbManager(SqlServerTestsConfiguration);
            string testDbName = newDbName;

            var connectionString = sqlServerTestDbManager.BuildConnectionString(testDbName);
            TestDbConnectionString testDbConnectionString = new TestDbConnectionString(connectionString, title);

            DatabaseBackupInfo databaseBackupInfo;
            if (cacheKey != null)
            {
                if (Cache.TryGetValue(cacheKey, out databaseBackupInfo))
                {
                    Stopwatch restoreAt = Stopwatch.StartNew();
                    PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Restoring DB '{testDbName}' from Backup '{databaseBackupInfo.BackupName}'");
                    await sqlServerTestDbManager.RestoreBackup(databaseBackupInfo, testDbName);
                    PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] DB '{testDbName}' Successfully Restored in {restoreAt.Elapsed.TotalSeconds:n3} seconds from Backup '{databaseBackupInfo.BackupName}'");

                    return testDbConnectionString;
                }
            }

            PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Creating new test DB '{testDbName}' (Caching key is '{cacheKey}')");
            await sqlServerTestDbManager.CreateEmptyDatabase(testDbName);
            PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Populating DB '{testDbName}' by Migrate and Seed");
            actionMigrateAndSeed(testDbConnectionString);

            if (cacheKey != null)
            {
                databaseBackupInfo = await sqlServerTestDbManager.CreateBackup(cacheKey, testDbName);
                PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Created Backup for test DB '{testDbName}' as '{databaseBackupInfo.BackupName}' (Caching key is '{cacheKey}')");
                // TODO: Dispose the Backup
                TestCleaner.OnDispose($"Drop Backup {databaseBackupInfo.BackupName}", () => File.Delete(databaseBackupInfo.BackupName), TestDisposeOptions.AsyncGlobal);
                Cache[cacheKey] = databaseBackupInfo;
            }

            return testDbConnectionString;
        }
    }
}
