using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ErgoFab.DataAccess.IntegrationTests.Library
{
    public class SeededDatabaseFactory
    {
        public readonly ISqlServerTestsConfiguration SqlServerTestsConfiguration;

        private static readonly ConcurrentDictionary<string, DatabaseBackupInfo> Cache = new ConcurrentDictionary<string, DatabaseBackupInfo>();

        public SeededDatabaseFactory(ISqlServerTestsConfiguration sqlServerTestsConfiguration)
        {
            SqlServerTestsConfiguration = sqlServerTestsConfiguration;
        }


        // 1. cacheKey is bound to actionMigrate+actionSeed
        // 2. Used by tests only. Thus gets DbConnectionString instead of IDbConnectionString
        public async Task<DbConnectionString> BuildDatabase(string cacheKey, string title, string newDbName, Action<IDbConnectionString> actionMigrate, Action<IDbConnectionString> actionSeed)
        {
            
            SqlServerTestDbManager sqlServerTestDbManager = new SqlServerTestDbManager(SqlServerTestsConfiguration);
            string testDbName = newDbName;
            DbConnectionString dbConnectionString = new DbConnectionString(testDbName, title);

            if (Cache.TryGetValue(cacheKey, out var databaseBackupInfo))
            {
                await sqlServerTestDbManager.RestoreBackup(databaseBackupInfo, testDbName);
                return dbConnectionString;
            }

            await sqlServerTestDbManager.CreateEmptyDatabase(testDbName);
            actionMigrate(dbConnectionString);

            actionSeed(dbConnectionString);

            databaseBackupInfo = await sqlServerTestDbManager.CreateBackup(cacheKey, testDbName);
            Cache[cacheKey] = databaseBackupInfo;
            return dbConnectionString;
        }
    }
}
