using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using ErgoFab.DataAccess.IntegrationTests.Library;
using Universe.SqlServerJam;

namespace Universe.SqlInsights.NUnit
{
    // TODO: Move to DI
    public class SqlServerTestDbManager
    {
        public virtual ISqlServerTestsConfiguration SqlTestsConfiguration { get;  }

        public SqlServerTestDbManager(ISqlServerTestsConfiguration sqlTestsConfiguration)
        {
            SqlTestsConfiguration = sqlTestsConfiguration;
        }

        static string EscapeSqlString(string arg) => $"'{arg.Replace("'", "''")}'";

        public virtual async Task CreateEmptyDatabase(IDbConnectionString dbConnectionString)
        {
            var databases = await GetDatabaseNames();
            var dbName = this.GetDatabaseName(dbConnectionString.ConnectionString);
            if (databases.Any(x => x.Equals(dbName, StringComparison.OrdinalIgnoreCase))) return;
            await CreateEmptyDatabase(dbName);
        }

        public virtual async Task CreateEmptyDatabase(string name)
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

        public virtual async Task DropDatabase(string name)
        {
            var cs = BuildConnectionString(name, false);
            AgileDbKiller.Kill(cs, false, 1);
        }
        public virtual async Task DropDatabase_Ugly(string name)
        {
            var sql1 = @$"If Exists (Select 1 From sys.databases where name={EscapeSqlString(name)}) And (SERVERPROPERTY('EngineEdition') <> 5) ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
            var sql2 = @$"If Exists (Select 1 From sys.databases where name={EscapeSqlString(name)}) Drop Database [{name}];";
            using (var masterConnection = this.CreateMasterConnection(true))
            {
                await masterConnection.ExecuteAsync(sql1);
                await masterConnection.ExecuteAsync(sql2);
            }
        }

        public virtual string BuildConnectionString(string dbName, bool pooling = true)
        {
            var b = CreateDbProviderFactory().CreateConnectionStringBuilder();
            b.ConnectionString = this.SqlTestsConfiguration.MasterConnectionString;
            b["Initial Catalog"] = dbName;
            b["Pooling"] = pooling.ToString();
            b["Application Name"] = SqlTestsConfiguration.DbName + " Test";
            return b.ConnectionString;
        }

        public virtual async Task<string[]> GetDatabaseNames()
        {
            var dbConnection = CreateMasterConnection();
            var ret = await dbConnection.QueryAsync<string>("Select name from sys.databases");
            return ret.ToArray();
        }

        public virtual DbConnection CreateMasterConnection(bool pooling = true)
        {
            var csb = CreateDbProviderFactory().CreateConnectionStringBuilder();
            csb.ConnectionString = SqlTestsConfiguration.MasterConnectionString;
            csb["Pooling"] = pooling;
            csb["Application Name"] = SqlTestsConfiguration.DbName + " Test";
            var dbConnection = CreateDbProviderFactory().CreateConnection();
            dbConnection.ConnectionString = csb.ConnectionString;
            return dbConnection;
        }

        public virtual DbProviderFactory CreateDbProviderFactory()
        {
            if (SqlTestsConfiguration.Provider == "Microsoft")
                // throw new NotImplementedException("TODO: Add reference and return corresponding DbProviderFactory");
                return Microsoft.Data.SqlClient.SqlClientFactory.Instance;

            else if (SqlTestsConfiguration.Provider == "System")
                return SqlClientFactory.Instance;

            throw new InvalidOperationException($"Unknown DB Provider '{SqlTestsConfiguration.Provider}'. Supported are Microsoft|System");
        }

        public virtual async Task<DatabaseBackupInfo> CreateBackup(string cacheKey, string dbName)
        {
            var masterConnection = CreateMasterConnection();
            var withCompression = masterConnection.Manage().IsCompressedBackupSupported ? "COMPRESSION, " : "";

            var bakName = Path.Combine(this.SqlTestsConfiguration.BackupFolder, $"{cacheKey}.bak");

            // WITH NOFORMAT, INIT,  NAME = N'Ergo Fab-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, COMPRESSION, STATS = 10
            string sql = $"BACKUP DATABASE [{dbName}] TO DISK = N{EscapeSqlString(bakName)} WITH {withCompression} NOFORMAT, INIT, NAME = N'For Tests Temporary Cache'";
            TryAndForget.Execute(() => Directory.CreateDirectory(this.SqlTestsConfiguration.BackupFolder));
            await masterConnection.ExecuteAsync(sql, commandTimeout: 180);
            var backupDescription = masterConnection.Manage().GetBackupDescription(bakName);
            return new DatabaseBackupInfo()
            {
                BackupName = bakName,
                BackupFiles = backupDescription.FileList.ToArray(),
            };
        }

        public virtual async Task RestoreBackup(DatabaseBackupInfo databaseBackupInfo, string dbName)
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

    public static class ConnectionStringExtensions
    {
        public static string GetDatabaseName(this SqlServerTestDbManager man, string connectionString)
        {
            var b = man.CreateDbProviderFactory().CreateConnectionStringBuilder();
            b.ConnectionString = connectionString;
            var dbName = b["Initial Catalog"]?.ToString();
            return dbName;
        }

    }
}
