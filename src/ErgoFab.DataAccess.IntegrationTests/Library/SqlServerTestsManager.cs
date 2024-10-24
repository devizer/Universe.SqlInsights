using System.Data.Common;
using System.Data.SqlClient;
using Dapper;
using ErgoFab.DataAccess.IntegrationTests.Shared;
using Universe.SqlServerJam;

namespace ErgoFab.DataAccess.IntegrationTests.Library
{
    public class SqlServerTestsManager
    {
        private readonly ISqlServerTestsConfiguration SqlTestsConfiguration;

        public SqlServerTestsManager(ISqlServerTestsConfiguration sqlTestsConfiguration)
        {
            SqlTestsConfiguration = sqlTestsConfiguration;
        }

        public async Task<string> GetNextTestDatabaseName()
        {
            string dbPrefix = $"{SqlTestsConfiguration.DbName} Test {DateTime.Now.ToString("yyyy-MM-dd")}";
            var allNames = await GetDatabaseNames();
            var setNames = allNames.ToHashSet();
            for (int i = 1; i < 10000; i++)
            {
                string ret = $"{dbPrefix} {i:0000}";
                if (!setNames.Contains(ret)) return ret;
            }

            throw new Exception("No Free database names");
        }

        static string EscapeSqlString(string arg) => $"'{arg.Replace("'", "''")}'";

        public async Task CreateEmptyDatabase(string name)
        {
            var mdf = Path.Combine(SqlServerTestsConfiguration.Instance.DatabaseDataFolder, $"{name}.mdf");
            var ldf = Path.Combine(SqlServerTestsConfiguration.Instance.DatabaseLogFolder, $"{name}.ldf");

            var sql1 = $@"Create Database [{name}] 
On (NAME = {EscapeSqlString($"{name} mdf")}, FILENAME = {EscapeSqlString(mdf)} /*, SIZE = 8192KB, FILEGROWTH = 8192KB */) 
LOG On (NAME = {EscapeSqlString($"{name} ldf")}, FILENAME =  {EscapeSqlString(ldf)} /*, SIZE = 8192KB, FILEGROWTH = 8192KB */)";

            var sql2 = $@"Alter Database [{name}] Set Recovery Simple";

            var masterConnection = CreateMasterConnection();

            TryAndForget.Execute(() => Directory.CreateDirectory(Path.GetDirectoryName(mdf)));
            TryAndForget.Execute(() => Directory.CreateDirectory(Path.GetDirectoryName(ldf)));

            await masterConnection.ExecuteAsync(sql1, new { mdfName = $"{name} mdf", mdfFullName = mdf, ldfName = $"{name} ldf", ldfFullName = ldf, });
            await masterConnection.ExecuteAsync(sql2);
        }

        public async Task DropDatabase(string name)
        {
            await this.CreateMasterConnection().ExecuteAsync($"Drop Database [{name}]");
        }

        public string BuildConnectionString(string dbName, bool pooling = true)
        {
            var b = CreateDbProviderFactory().CreateConnectionStringBuilder();
            b.ConnectionString = this.SqlTestsConfiguration.MasterConnectionString;
            b["Initial Catalog"] = dbName;
            b["Pooling"] = pooling.ToString();
            return b.ConnectionString;
        }

        public async Task<string[]> GetDatabaseNames()
        {
            var dbConnection = CreateMasterConnection();
            var ret = await dbConnection.QueryAsync<string>("Select name from sys.databases");
            return ret.ToArray();
        }

        public DbConnection CreateMasterConnection(bool pooling = true)
        {
            SqlConnectionStringBuilder b = new SqlConnectionStringBuilder(SqlTestsConfiguration.MasterConnectionString);
            b.Pooling = pooling;
            b.ApplicationName = SqlTestsConfiguration.DbName + " Test";

            var dbConnection = CreateDbProviderFactory().CreateConnection();
            dbConnection.ConnectionString = b.ConnectionString;
            return dbConnection;
        }

        public DbProviderFactory CreateDbProviderFactory()
        {
            return SqlClientFactory.Instance;
        }

        public async Task<DatabaseBackupInfo?> CreateBackup(string cacheKey, string dbName)
        {
            var masterConnection = CreateMasterConnection();
            var withCompression = masterConnection.Manage().IsCompressedBackupSupported ? "COMPRESSION, " : "";

            var bakName = Path.Combine(this.SqlTestsConfiguration.BackupFolder, $"{cacheKey}.bak");

            // WITH NOFORMAT, INIT,  NAME = N'Ergo Fab-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, COMPRESSION, STATS = 10
            string sql = $"BACKUP DATABASE [{dbName}] TO DISK = N{EscapeSqlString(bakName)} WITH {withCompression}, NOFORMAT, INIT, NAME = N'For Cache'";
            await masterConnection.ExecuteAsync(sql, commandTimeout: 180);
            return new DatabaseBackupInfo()
            {
                BackupName = bakName,
            };
        }

        public async Task RestoreBackup(DatabaseBackupInfo databaseBackupInfo, string testDbName)
        {
            throw new NotImplementedException();
        }
    }
}
