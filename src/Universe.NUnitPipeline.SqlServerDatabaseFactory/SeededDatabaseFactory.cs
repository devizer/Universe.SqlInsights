using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Universe.SqlServerJam;

namespace Universe.NUnitPipeline.SqlServerDatabaseFactory
{
    public class SeededDatabaseFactory
    {
        public readonly ISqlServerTestsConfiguration SqlServerTestsConfiguration;

        private static readonly ConcurrentDictionary<string, SqlBackupDescription> Cache = new();


        public SeededDatabaseFactory(ISqlServerTestsConfiguration sqlServerTestsConfiguration)
        {
            SqlServerTestsConfiguration = sqlServerTestsConfiguration;
        }


        public async Task<IDbConnectionString> BuildDatabase(string cacheKey, string newDbName, string title, Action<IDbConnectionString> actionMigrateAndSeed)
        {
            SqlServerTestDbManager sqlServerTestDbManager = new SqlServerTestDbManager(SqlServerTestsConfiguration);

            var connectionString = sqlServerTestDbManager.BuildConnectionString(newDbName);
            TestDbConnectionString testDbConnectionString = new TestDbConnectionString(connectionString, title);

            SqlBackupDescription databaseBackupInfo;
            if (cacheKey != null)
            {
                if (Cache.TryGetValue(cacheKey, out databaseBackupInfo))
                {
                    Stopwatch restoreAt = Stopwatch.StartNew();
                    PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Restoring DB '{newDbName}' from Backup '{databaseBackupInfo.BackupPoint}'");
                    await sqlServerTestDbManager.RestoreBackup(databaseBackupInfo, newDbName);
                    PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] DB '{newDbName}' Successfully Restored in {restoreAt.Elapsed.TotalSeconds:n3} seconds from Backup '{databaseBackupInfo.BackupPoint}'");

                    return testDbConnectionString;
                }
            }

            PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Creating new test DB '{newDbName}' (Caching key is '{cacheKey}')");
            await sqlServerTestDbManager.CreateEmptyDatabase(newDbName);
            PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Populating DB '{newDbName}' by Migrate and Seed");
            actionMigrateAndSeed(testDbConnectionString);

            if (cacheKey != null)
            {
                databaseBackupInfo = await sqlServerTestDbManager.CreateBackup(cacheKey, newDbName);
                PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Created Backup for test DB '{newDbName}' as '{databaseBackupInfo.BackupPoint}' (Caching key is '{cacheKey}')");
                // Dispose the Backup
                TestCleaner.OnDispose($"Drop Backup {databaseBackupInfo.BackupPoint}", () => File.Delete(databaseBackupInfo.BackupPoint), TestDisposeOptions.AsyncGlobal);
                Cache[cacheKey] = databaseBackupInfo;
            }

            return testDbConnectionString;
        }
    }
}
