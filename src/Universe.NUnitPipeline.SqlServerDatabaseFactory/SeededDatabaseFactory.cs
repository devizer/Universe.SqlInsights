using Dapper;
using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Universe.SqlServerJam;

namespace Universe.NUnitPipeline.SqlServerDatabaseFactory
{
    public class SeededDatabaseFactory
    {
        public readonly ISqlServerTestsConfiguration SqlServerTestsConfiguration;

        private static readonly ConcurrentDictionary<string, SqlBackupDescription> Cache = new();
        private static readonly ConcurrentDictionary<string, object> SyncForCreateDb = new();


        public SeededDatabaseFactory(ISqlServerTestsConfiguration sqlServerTestsConfiguration)
        {
            SqlServerTestsConfiguration = sqlServerTestsConfiguration;
        }


        public async Task<IDbConnectionString> BuildDatabase(string cacheKey, string newDbName, string title, string playgroundDatabaseName, Action<IDbConnectionString> actionMigrateAndSeed)
        {
            SqlServerTestDbManager sqlServerTestDbManager = new SqlServerTestDbManager(SqlServerTestsConfiguration);

            var connectionString = sqlServerTestDbManager.BuildConnectionString(newDbName);
            TestDbConnectionString testDbConnectionString = new TestDbConnectionString(connectionString, title);

            SqlBackupDescription databaseBackupInfo;
            if (cacheKey != null)
            {
                if (Cache.TryGetValue(cacheKey, out databaseBackupInfo))
                {
                    PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Restoring DB '{newDbName}' from Backup '{databaseBackupInfo.BackupPoint}'");
                    Stopwatch restoreAt = Stopwatch.StartNew();
                    await sqlServerTestDbManager.RestoreBackup(databaseBackupInfo, newDbName);
                    PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] DB '{newDbName}' Successfully Restored in {restoreAt.AsString()} from Backup '{databaseBackupInfo.BackupPoint}'");

                    // After restore we need to check up integrity. it takes a while on demand
                    using (var newConnection = sqlServerTestDbManager.CreateDbProviderFactory().CreateConnection(connectionString))
                    {
                        var dummy = newConnection.Manage().ShortServerVersion;
                        Stopwatch startWarmupAt = Stopwatch.StartNew();
                        WarmUpDbAfterRestore(newConnection);
                        PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] DB '{newDbName}' Warmed up in {startWarmupAt.AsString()}");
                    }

                    return testDbConnectionString;
                }
            }

            var syncObject = cacheKey != null ? SyncForCreateDb.GetOrAdd(cacheKey, new object()) : null;
#if NET35
            System.Runtime.CompilerServices.RuntimeHelpers.PrepareConstrainedRegions();
#endif
            try // lock
            {
                if (syncObject != null) Monitor.Enter(syncObject);
                PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Creating new test DB '{newDbName}' (Caching key is '{cacheKey}')");

                // Delete if exists
                Stopwatch startDeleteAt = Stopwatch.StartNew();
                ResilientDbKiller.Kill(sqlServerTestDbManager.SqlTestsConfiguration.MasterConnectionString, newDbName, false, 3);
                PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] DB '{newDbName}' optionally deleted in {startDeleteAt.AsString()}");

                // Create Empty
                sqlServerTestDbManager.CreateEmptyDatabase(newDbName).SafeWait();

                // Migrate and Seed
                PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Populating DB '{newDbName}' by Migrate and Seed");
                Stopwatch startMigrateAndSeedAt = Stopwatch.StartNew();
                actionMigrateAndSeed(testDbConnectionString);
                PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Migrate and Seed for DB '{newDbName}' completed in {startMigrateAndSeedAt.AsString()}");

                if (cacheKey != null)
                {
                    Stopwatch startCreateBackupAt = Stopwatch.StartNew();
                    databaseBackupInfo = sqlServerTestDbManager.CreateBackup(cacheKey, newDbName).GetSafeResult();
                    PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Backup for test DB '{newDbName}' created in {startCreateBackupAt.AsString()} as '{databaseBackupInfo.BackupPoint}' (Caching key is '{cacheKey}')");
                    // DONE: Skip playgroundDatabaseName?
                    if (playgroundDatabaseName != null && InternalDbFactoryTuningConfiguration.SkipTestSqlFactoryPlaygroundDatabase == false)
                    {
                        // First. Force Drop playgroundDatabaseName ...
                        var savedDbConnectionString = sqlServerTestDbManager.BuildConnectionString(playgroundDatabaseName);
                        ResilientDbKiller.Kill(savedDbConnectionString, false, 3);
                        // .. And Restore it from newly created backup
                        // await sqlServerTestDbManager.RestoreBackup(databaseBackupInfo, playgroundDatabaseName);
                        sqlServerTestDbManager.RestoreBackup(databaseBackupInfo, playgroundDatabaseName).SafeWait();
                        PipelineLog.LogTrace($"[SeededDatabaseFactory.BuildDatabase] Restored Reference Test DB '{playgroundDatabaseName}' from '{databaseBackupInfo.BackupPoint}' (Caching key is '{cacheKey}')");
                    }

                    // Dispose the Backup
                    TestCleaner.OnDispose($"Drop Backup {databaseBackupInfo.BackupPoint}", () => File.Delete(databaseBackupInfo.BackupPoint), TestDisposeOptions.AsyncGlobal);
                    Cache[cacheKey] = databaseBackupInfo;
                }
            }
            finally
            {
                if (syncObject != null) Monitor.Exit(syncObject);
            }

            return testDbConnectionString;
        }

        private void WarmUpDbAfterRestore(DbConnection newConnection)
        {
            // return;
            
            const string sql = @"Set NoCount On;
Declare tableList CURSOR STATIC FOR
SELECT '[' + TABLE_SCHEMA + '].[' + TABLE_NAME  + ']' as TableFullName FROM information_schema.tables Where Table_Type = 'BASE TABLE'

Declare @tableName nvarchar(1024)
Open tableList
While 1=1 Begin
  Fetch Next From tableList Into @tableName
  If @@fetch_status<>0 Break
  Print 'Warn Up Table ' + @tableName
  Exec ('Select Count(1) From ' + @tableName + ' Where 1=2')
End

Close tableList
Deallocate tableList
";

            newConnection.Query<int>(sql);
        }
    }
}

namespace Universe
{
    public static class StopwatchExtensions
    {
        public static string AsString(this Stopwatch stopwatch)
        {
            var ticks = stopwatch.ElapsedTicks;
            double seconds = ticks / (double)Stopwatch.Frequency;
            if (seconds < 0.2) return $"{seconds:f3}s";
            else if (seconds < 1) return $"{seconds:f2}s";
            else if (seconds < 60) return $"{seconds:f1}s";
            else if (seconds < 3600) return new DateTime(0).AddSeconds(seconds).ToString("mm:ss.f");
            else if (seconds < 24 * 3600) return new DateTime(0).AddSeconds(seconds).ToString("HH:mm:ss.f");
            else return TimeSpan.FromSeconds(seconds).ToString();
        }
    }
}
