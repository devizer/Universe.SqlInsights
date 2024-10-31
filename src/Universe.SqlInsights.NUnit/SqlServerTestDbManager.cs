using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Universe.SqlServerJam;

namespace ErgoFab.DataAccess.IntegrationTests.Library
{
    public class SqlServerTestDbManager
    {
        public readonly ISqlServerTestsConfiguration SqlTestsConfiguration;

        public SqlServerTestDbManager(ISqlServerTestsConfiguration sqlTestsConfiguration)
        {
            SqlTestsConfiguration = sqlTestsConfiguration;
        }

        static string EscapeSqlString(string arg) => $"'{arg.Replace("'", "''")}'";

        public async Task CreateEmptyDatabase(IDbConnectionString dbConnectionString)
        {
            var databases = await GetDatabaseNames();
            var dbName = new SqlConnectionStringBuilder(dbConnectionString.ConnectionString).InitialCatalog;
            if (databases.Contains(dbName)) return;
            await CreateEmptyDatabase(dbName);
        }

        public async Task CreateEmptyDatabase(string name)
        {
            var mdf = Path.Combine(this.SqlTestsConfiguration.DatabaseDataFolder, $"{name}.mdf");
            var ldf = Path.Combine(this.SqlTestsConfiguration.DatabaseLogFolder, $"{name}.ldf");

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
            if (SqlTestsConfiguration.Provider == "Microsoft")
                throw new NotImplementedException("TODO: Add reference and return corresponding DbProviderFactory");
            else if (SqlTestsConfiguration.Provider == "System")
                return SqlClientFactory.Instance;

            throw new InvalidOperationException($"Unknown DB Provider '{SqlTestsConfiguration.Provider}'. Supported are Microsoft|System");
        }

        public async Task<DatabaseBackupInfo> CreateBackup(string cacheKey, string dbName)
        {
            var masterConnection = CreateMasterConnection();
            var withCompression = masterConnection.Manage().IsCompressedBackupSupported ? "COMPRESSION, " : "";

            var bakName = Path.Combine(this.SqlTestsConfiguration.BackupFolder, $"{cacheKey}.bak");

            // WITH NOFORMAT, INIT,  NAME = N'Ergo Fab-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, COMPRESSION, STATS = 10
            string sql = $"BACKUP DATABASE [{dbName}] TO DISK = N{EscapeSqlString(bakName)} WITH {withCompression} NOFORMAT, INIT, NAME = N'For Cache'";
            TryAndForget.Execute(() => Directory.CreateDirectory(this.SqlTestsConfiguration.BackupFolder));
            await masterConnection.ExecuteAsync(sql, commandTimeout: 180);
            var backupDescription = masterConnection.Manage().GetBackupDescription(bakName);
            return new DatabaseBackupInfo()
            {
                BackupName = bakName,
                BackupFiles = backupDescription.FileList.ToArray(),
            };
        }

        public async Task RestoreBackup(DatabaseBackupInfo databaseBackupInfo, string dbName)
        {
            var sql = new StringBuilder($"Restore Database [{dbName}] From Disk = N'{databaseBackupInfo.BackupName}' With ");
            var index = 0;
            foreach (var f in databaseBackupInfo.BackupFiles)
            {
                index++;
                var folder = f.StrictType == BackFileType.Log ? this.SqlTestsConfiguration.DatabaseLogFolder : this.SqlTestsConfiguration.DatabaseDataFolder;
                var physicalPath = Path.Combine(folder, $"{dbName}.{index}.{f.StrictType}");
                string sqlMove = $" MOVE N{EscapeSqlString(f.LogicalName)} TO N{EscapeSqlString(physicalPath)},";
                sql.Append(sqlMove);
            }

            sql.Append(" Replace");

            var masterConnection = CreateMasterConnection();
            await masterConnection.ExecuteAsync(sql.ToString(), commandTimeout: 180);
        }
    }
}
