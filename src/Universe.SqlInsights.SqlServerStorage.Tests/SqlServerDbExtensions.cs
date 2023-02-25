using System.Data.SqlClient;
using System.IO;
using Dapper;

namespace Universe.SqlInsights.SqlServerStorage.Tests
{
    public static class SqlServerDbExtensions
    {
        public static void CreateDbIfNotExists(string connectionString, string dbDataDir = null, int initialDataSize = 64, int initialLogSize = 32)
        {
            var master = new SqlConnectionStringBuilder(connectionString);
            var db = master.InitialCatalog;
            master.InitialCatalog = "";
            using (var con = new SqlConnection(master.ConnectionString))
            {
                var existingName = con.QueryFirstOrDefault<string>("Select name from sys.databases where name=@db", new { db });
                if (existingName == null)
                {
                    string sql = $@"CREATE DATABASE [{db}]";
                    if (!string.IsNullOrEmpty(dbDataDir))
                    {
                        string datFile = Path.Combine(dbDataDir, $"{db}.mdf"); 
                        string logFile = Path.Combine(dbDataDir, $"{db}.ldf");
                        Directory.CreateDirectory(dbDataDir);
                        sql += $@"
ON (NAME = [{db}_dat], FILENAME = '{datFile}', SIZE = {initialDataSize}MB, FILEGROWTH = 8MB) 
LOG ON (NAME = [{db}_log], FILENAME = '{logFile}', SIZE = {initialLogSize}MB, FILEGROWTH = 8MB ); 
";
                    }
 
                    con.Execute(sql);
                    con.Execute($"Alter Database [{db}] Set Recovery Simple;");
                }
            }
        }
    }
}