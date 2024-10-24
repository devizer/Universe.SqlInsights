using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IDbConnectionString> BuildEfDatabase<TDbContext>(string cacheKey, string title, Func<IDbConnectionString, TDbContext> createDbContext, Action<IDbConnectionString> actionSeed) where TDbContext : DbContext
        {
            void Migrate(IDbConnectionString dbConnection)
            {
                var dbContext = createDbContext(dbConnection);
                dbContext.Database.Migrate();
            }

            return await BuildDatabase(cacheKey, title, Migrate, actionSeed);
        }

        // TODO: Bind to OrganozationTests
        public async Task<IDbConnectionString> BuildDatabase(string cacheKey, string title, Action<IDbConnectionString> actionMigrate, Action<IDbConnectionString> actionSeed)
        {
            
            SqlServerTestsManager sqlServerTestsManager = new SqlServerTestsManager(SqlServerTestsConfiguration);
            var testDbName = await sqlServerTestsManager.GetNextTestDatabaseName();
            DbConnectionString dbConnectionString = new DbConnectionString(testDbName, title);

            DatabaseBackupInfo databaseBackupInfo;
            if (Cache.TryGetValue(cacheKey, out databaseBackupInfo))
            {
                await sqlServerTestsManager.RestoreBackup(databaseBackupInfo, testDbName);
                return dbConnectionString;
            }

            await sqlServerTestsManager.CreateEmptyDatabase(testDbName);
            actionMigrate(dbConnectionString);

            actionSeed(dbConnectionString);

            databaseBackupInfo = await sqlServerTestsManager.CreateBackup(cacheKey, testDbName);
            Cache[cacheKey] = databaseBackupInfo;
            return dbConnectionString;
        }
    }
}
