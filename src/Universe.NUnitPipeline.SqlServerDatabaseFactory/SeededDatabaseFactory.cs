using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Dapper;
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


        public async Task<IDbConnectionString> BuildDatabase(string cacheKey, string newDbName, string title, string savedDatabaseName, Action<IDbConnectionString> actionMigrateAndSeed)
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

                    // After restore we need to check up integrity. it takes a while on demand
                    using (var newConnection = sqlServerTestDbManager.CreateDbProviderFactory().CreateConnection(connectionString))
                    {
                        var dummy = newConnection.Manage().ShortServerVersion;
                        WarmUpDbAfterRestore(newConnection);
                    }

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
                if (savedDatabaseName != null)
                {
                    await sqlServerTestDbManager.RestoreBackup(databaseBackupInfo, savedDatabaseName);
                    PipelineLog.LogTrace(
                        $"[SeededDatabaseFactory.BuildDatabase] Restored Reference Test DB '{savedDatabaseName}' from '{databaseBackupInfo.BackupPoint}' (Caching key is '{cacheKey}')");
                }

                // Dispose the Backup
                TestCleaner.OnDispose($"Drop Backup {databaseBackupInfo.BackupPoint}", () => File.Delete(databaseBackupInfo.BackupPoint), TestDisposeOptions.AsyncGlobal);
                Cache[cacheKey] = databaseBackupInfo;
            }

            return testDbConnectionString;
        }

        private void WarmUpDbAfterRestore(DbConnection newConnection)
        {
            const string sql = @"Set NoCount On;
declare @tableName nvarchar(1024)
declare tableList CURSOR STATIC FOR 
SELECT '[' + TABLE_SCHEMA + '].[' + TABLE_NAME  + ']' as TableFullName FROM information_schema.tables Where Table_Type = 'BASE TABLE'
		
open tableList
while 1=1 begin
  fetch next from tableList into @tableName
  if @@fetch_status<>0 break
  print 'Table ' + @tableName
  exec ('Select count(1) From ' + @tableName + ' Where 1=2')
end

close tableList
deallocate tableList
";

            newConnection.Query<int>(sql);
        }
    }
}
