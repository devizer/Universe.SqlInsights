﻿using System.Collections.Concurrent;
using ErgoFab.DataAccess.IntegrationTests.Shared;

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
        public async Task<IDbConnectionString> BuildDatabase(string cacheKey, string newDbName, string title, Action<IDbConnectionString> actionMigrate, Action<IDbConnectionString> actionSeed)
        {
            SqlServerTestDbManager sqlServerTestDbManager = new SqlServerTestDbManager(SqlServerTestsConfiguration);
            // string testDbName = await sqlServerTestDbManager.GetNextTestDatabaseName();
            string testDbName = newDbName;

            DbConnectionString dbConnectionString = new DbConnectionString(testDbName, title);

            ;
            if (Cache.TryGetValue(cacheKey, out DatabaseBackupInfo databaseBackupInfo))
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
