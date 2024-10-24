using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ErgoFab.DataAccess.IntegrationTests.Library;

namespace ErgoFab.DataAccess.IntegrationTests.Shared
{
    internal class SqlServerTestsConfiguration : ISqlServerTestsConfiguration
    {
        public static readonly SqlServerTestsConfiguration Instance = new SqlServerTestsConfiguration();
        public string DbName { get; } = "Ergo Fab";
        public string MasterConnectionString { get; } = "Data Source=(local);Integrated Security=True;Pooling=true;Timeout=30;TrustServerCertificate=True;";
        public string BackupFolder { get; } = "W:\\Temp\\Integration Tests\\Backups";
        public string DatabaseDataFolder { get; } = "W:\\Temp\\Integration Tests";
        public string DatabaseLogFolder { get; } = "W:\\Temp\\Integration Tests";

        public string Provider { get; } = "System";
    }
}
