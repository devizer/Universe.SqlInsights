using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using Dapper;
using Universe.SqlServerJam;

namespace Universe.SqlInsights.SqlServerStorage
{
    public class SqlServerSqlInsightsMigrations
    {
        public readonly DbProviderFactory ProviderFactory;
        public readonly string ConnectionString;
        public readonly StringBuilder Logs = new StringBuilder();
        public bool ThrowOnDbCreationError { get; set; } = false;

        // public static volatile bool DisableMemoryOptimizedTables;
        public static bool DisableMemoryOptimizedTables
        {
            get => true;
            set { }
        }

        public SqlServerSqlInsightsMigrations(DbProviderFactory providerFactory, string connectionString)
        {
            ProviderFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public List<string> GetSqlMigrations()
        {
            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(ConnectionString);
            string dbName = sqlConnectionStringBuilder.InitialCatalog;
            string serverName = sqlConnectionStringBuilder.InitialCatalog;
            Logs.AppendLine($" * DisableMemoryOptimizedTables: {DisableMemoryOptimizedTables}");
            Logs.AppendLine($" * Db Name: [{dbName}]");
            
            IDbConnection cnn = this.ProviderFactory.CreateConnection();
            cnn.ConnectionString = this.ConnectionString;
            var man = cnn.Manage();
            Logs.AppendLine($" * IsLocalDB: {man.IsLocalDB}");
            Logs.AppendLine($" * Short Version: {man.ShortServerVersion} on {man.HostPlatform}");
            Logs.AppendLine($" * Medium Version: {man.MediumServerVersion}");
            Logs.AppendLine($" * Long Version: {man.LongServerVersion}");

            if (man.IsWindows && (man.FixedServerRoles & FixedServerRoles.SysAdmin) != 0)
            {
                // TODO: Only if ISqlInsightsConfiguration.DisposeByShellCommand == true
                man.ServerConfigurationSettings.XpCmdShell = true;
                Logs.AppendLine($" * Allow XpCmdShell: {man.ServerConfigurationSettings.XpCmdShell}");
            }

            // Server 2016 (13.x) SP1 (or later), any edition. For SQL Server 2014 (12.x) and SQL Server 2016 (13.x) RTM (pre-SP1) you need Enterprise, Developer, or Evaluation edition.
            var supportMOT = man.IsMemoryOptimizedTableSupported;
            Logs.AppendLine($" * Support MOT: {supportMOT}{(DisableMemoryOptimizedTables & supportMOT ? ", But Disabled": "")}");

            if (DisableMemoryOptimizedTables) supportMOT = false;
            
            // MOT Folder
            var sampleFile = cnn.Query<string>("Select Top 1 filename from sys.sysfiles").FirstOrDefault();
            var dataFolder = sampleFile != null ? CrossPath.GetDirectoryName(man.IsWindows, sampleFile) : null;
            var motFileFolder = CrossPath.Combine(man.IsWindows, dataFolder, $"MOT for {dbName}");
            Logs.AppendLine($" * MOT Files Folder: {dataFolder}");

            var existingTables = cnn.Query<string>("Select name from SYSOBJECTS WHERE xtype = 'U' and name like '%SqlInsights%'").ToArray();
            var existingTablesInfo = existingTables.Length == 0 ? ">Not Found<" : String.Join(",", existingTables.Select(x => $"[{x}]").ToArray());
            Logs.AppendLine($" * Existing Tables: {existingTablesInfo}");

            string motFileGroup = null;
            if (supportMOT)
            {
                motFileGroup = cnn.Query<string>("Select Top 1 name from sys.filegroups where type = 'FX'").FirstOrDefault();
            }
            bool isMotFileGroupExists = !string.IsNullOrEmpty(motFileGroup);
            Logs.AppendLine($" * Existing MOT File Group Name: {(isMotFileGroupExists ? $"'{motFileGroup}'" : ">Not Found<")}");
            
            List<string> sqlConfigureMotList = new List<string>();
            if (supportMOT && !isMotFileGroupExists)
            {
                string sqlAutoCloseOff = @$"
ALTER DATABASE [{dbName}] 
SET AUTO_CLOSE OFF;";
                
                string sqlAddMotFileGroup = @$"
ALTER DATABASE [{dbName}]
ADD FILEGROUP MemoryOptimizedTablesFileGroup
CONTAINS MEMORY_OPTIMIZED_DATA;";

                string sqlAddMotFile = @$"
ALTER DATABASE [{dbName}] ADD FILE (
    name='SqlInsight MemoryOptimizedTables', filename='{motFileFolder}')
TO FILEGROUP MemoryOptimizedTablesFileGroup;";

                string sqlEnableTransactions = $@"
ALTER DATABASE [{dbName}]
SET MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT = ON;
";
                sqlConfigureMotList.AddRange(new[] { sqlAutoCloseOff, sqlAddMotFileGroup, sqlAddMotFile, sqlEnableTransactions});
            }
            
            string sqlWithMemory = supportMOT ? " WITH (MEMORY_OPTIMIZED=ON, DURABILITY=SCHEMA_ONLY)" : "";

            Func<string, string> IfMemory = sql => supportMOT ? sql : "";  
            Func<string, string> IfLegacy = sql => !supportMOT ? sql : "";  
            
            List<string> sqlCreateTablesList = new List<string>
            {
                // TODO: Add Tests for LONG app name or host name
                // Table SqlInsights String
                @$"
-- for MEMORY OPTIMIZED foreign keys should be replaced by indexes  
If Object_ID('SqlInsightsString') Is Null
BEGIN
Create Table SqlInsightsString(
    IdString bigint Identity Not Null,
    Kind tinyint Not Null, -- 1: KeyPath, 2: AppName, 3: HostId
    StartsWith nvarchar({StringsStorage.MaxStartLength}) Not Null,
    Tail nvarchar(max) Null
    {IfMemory(", Constraint PK_SqlInsightsString Primary Key NonClustered (IdString)")}
    {IfLegacy(", Constraint PK_SqlInsightsString Primary Key NonClustered (IdString)")}
){sqlWithMemory};
If Not Exists (Select 1 From sys.indexes Where name='UCX_SqlInsightsString_Kind_StartsWith')
{IfLegacy($@"Create Unique CLUSTERED Index UCX_SqlInsightsString_Kind_StartsWith On SqlInsightsString(Kind, StartsWith);")}
{IfMemory($@"ALTER TABLE SqlInsightsString ADD INDEX UCX_SqlInsightsString_Kind_StartsWith (Kind, StartsWith);")}    
END
-- DONE.
",

                // Table SqlInsights KeyPathSummaryTimestamp
                // This is workaround for memory optimized SqlInsightsKeyPathSummary 
                @$"
If Object_ID('SqlInsightsKeyPathSummaryTimestamp') Is Null
Create Table SqlInsightsKeyPathSummaryTimestamp(
    Version BigInt Not Null,
    Guid UniqueIdentifier Not Null,
    Constraint PK_SqlInsightsKeyPathSummaryTimestamp Primary Key {(supportMOT ? "NON" : "")}Clustered (Guid)
){sqlWithMemory};
If Not Exists(Select Version From SqlInsightsKeyPathSummaryTimestamp)
Insert SqlInsightsKeyPathSummaryTimestamp(Version, Guid) Values(0, NewId());
-- DONE.",

                // Table SqlInsights Session 
                @$"
If Object_ID('SqlInsightsSession') Is Null
Begin
Create Table SqlInsightsSession(
    IdSession bigint Identity Not Null,
    StartedAt DateTime Not Null,
    EndedAt DateTime Null,
    IsFinished bit Not Null,
    Caption nvarchar(1000) Not Null,
    MaxDurationMinutes int Null, 
    Constraint PK_SqlInsightsSession Primary Key {(supportMOT ? "NON" : "")}Clustered (IdSession)
){sqlWithMemory};
SET IDENTITY_INSERT SqlInsightsSession ON;
Insert SqlInsightsSession(IdSession, StartedAt, IsFinished, Caption) Values(
    0,
    GetUtcDate(),
    (0),
    'Lifetime session'
);    
SET IDENTITY_INSERT SqlInsightsSession OFF;
End
-- DONE.",

                // SqlInsights KeyPathSummary 
                @$"
If Object_ID('SqlInsightsKeyPathSummary') Is Null
Begin
Create Table SqlInsightsKeyPathSummary(
    KeyPath nvarchar(450) Not Null,
    AppName bigint Not Null,
    HostId bigint Not Null,
    IdSession bigint Not Null,
    Version BigInt Not Null,
    Data nvarchar(max) Not Null,
    Constraint PK_SqlInsightsKeyPathSummary Primary Key {(supportMOT ? "NON" : "")}Clustered (KeyPath, IdSession, AppName, HostId),
    Constraint FK_SqlInsightsKeyPathSummary_SqlInsightsSession FOREIGN KEY (IdSession) REFERENCES SqlInsightsSession(IdSession),
    Constraint FK_SqlInsightsKeyPathSummary_AppName FOREIGN KEY (AppName) REFERENCES SqlInsightsString(IdString),
    Constraint FK_SqlInsightsKeyPathSummary_HostId FOREIGN KEY (HostId) REFERENCES SqlInsightsString(IdString)
){sqlWithMemory};
{IfLegacy("Create Index IX_SqlInsightsKeyPathSummary_Version On SqlInsightsKeyPathSummary(Version Desc);")}
{IfMemory("Alter Table SqlInsightsKeyPathSummary Add Index IX_SqlInsightsKeyPathSummary_Version (Version Desc);")}
End
-- DONE.",

                // Table SqlInsights Action
                @$"
If Object_ID('SqlInsightsAction') Is Null
Begin
Create Table SqlInsightsAction(
    IdAction bigint Identity Not Null,
    IdSession bigint Not Null,
    At DateTime Not Null,
    KeyPath nvarchar(450) Not Null,
    AppName bigint Not Null,
    HostId bigint Not Null,
    IsOK bit Not Null,
    Data nvarchar(max) Not Null,
    -- Constraint PK_SqlInsightsAction Primary Key (IdAction),
    Constraint PK_SqlInsightsAction Primary Key {(supportMOT ? "NON" : "")}Clustered (KeyPath, IdSession, IdAction)
{IfLegacy($@"
    ,
    Constraint FK_SqlInsightsAction_SqlInsightsSession FOREIGN KEY (IdSession) REFERENCES SqlInsightsSession(IdSession),
    -- Do we need this FK? 
    Constraint FK_SqlInsightsAction_SqlInsightsKeyPathSummary FOREIGN KEY (KeyPath, IdSession, AppName, HostId) REFERENCES SqlInsightsKeyPathSummary(KeyPath, IdSession, AppName, HostId),
    -- Next joins are NEVER used
    -- Constraint FK_SqlInsightsAction_AppName FOREIGN KEY (AppName) REFERENCES SqlInsightsString(IdString), 
    -- Constraint FK_SqlInsightsAction_HostId FOREIGN KEY (HostId) REFERENCES SqlInsightsString(IdString),
")} 
);
CREATE NONCLUSTERED INDEX [IX_SqlInsightsAction_IdSession_KeyPath_IdAction_Data]
    ON [dbo].[SqlInsightsAction] ([IdSession],[KeyPath])
    INCLUDE ([IdAction],[Data]);
End 
"
            };

            List<string> ret = new List<string>();
            ret.AddRange(sqlConfigureMotList);
            ret.AddRange(sqlCreateTablesList);
            return ret;
        }

        public void Migrate()
        {
            CreateDatabaseIfNotExists();

            // Object_ID Returns null in case of missed permissions    
            using (var con = this.ProviderFactory.CreateConnection())
            {
                con.ConnectionString = this.ConnectionString;
                var sqlMigrations = GetSqlMigrations();
                foreach (var sqlMigration in sqlMigrations)
                {
                    try
                    {
                        con.Execute(sqlMigration, null);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Migration failed. Code: {Environment.NewLine}{sqlMigration}{Environment.NewLine}Diagnostic Details{Environment.NewLine}{this.Logs}", ex);
                    }
                }

                Logs.Append($" * Done! Migration successfully invoked {sqlMigrations.Count} commands");
            }
        }

        private void CreateDatabaseIfNotExists()
        {
            // var master = this.ProviderFactory.CreateConnectionStringBuilder();
            // master.ConnectionString = ConnectionString;
            SqlConnectionStringBuilder master = new SqlConnectionStringBuilder(ConnectionString);
            var dbName = master.InitialCatalog;
            if (string.IsNullOrEmpty(dbName))
                return;
            
            master.InitialCatalog = "";
            
            string sqlCommands = @$"
Select DB_ID('{dbName}');
If DB_ID('{dbName}') Is Null 
Begin 
    Create Database [{dbName}]; 
    -- The scenario is for development only
exec('Alter Database [{dbName}] Set Recovery Simple'); 
End
";

            try
            {
                var con = ProviderFactory.CreateConnection();
                con.ConnectionString = master.ConnectionString;
                using (con)
                {
                    con.Execute(sqlCommands, null);
                    /*
                    var existingDbId = con.Query<int?>(sqlCommands, null, null, true, 10, CommandType.Text);
                    if (existingDbId == null)
                        Logs.AppendLine($" * New database [{dbName}] successfully created on server {master.DataSource}");
                    else
                        Logs.AppendLine($" * Existing database [{dbName}] exists on server {master.DataSource}");
                */
                }
            }
            catch
            {
                // It's OK. Not enough permissions to the master DB or existing database.
                // SqlMigrations scripts will show such reason.
                if (ThrowOnDbCreationError) throw;
            }
        }
        

    }
}