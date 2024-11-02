using System.Collections.Concurrent;
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
                    PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Restoring DB '{testDbName}' from Backup '{databaseBackupInfo.BackupName}'");
                    await sqlServerTestDbManager.RestoreBackup(databaseBackupInfo, testDbName);
                    return testDbConnectionString;
                }
            }

            PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Creating new test DB '{testDbName}'");
            await sqlServerTestDbManager.CreateEmptyDatabase(testDbName);
            PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Populating DB '{testDbName}' by Migrate and Seed");
            actionMigrateAndSeed(testDbConnectionString);

            if (cacheKey != null)
            {
                databaseBackupInfo = await sqlServerTestDbManager.CreateBackup(cacheKey, testDbName);
                // TODO: Dispose the Backup
                Cache[cacheKey] = databaseBackupInfo;
            }

            return testDbConnectionString;
        }
    }
}
