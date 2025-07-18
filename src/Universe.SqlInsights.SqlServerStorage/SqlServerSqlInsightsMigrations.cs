﻿using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Universe.SqlServerJam;

namespace Universe.SqlInsights.SqlServerStorage
{
    public class SqlServerSqlInsightsMigrations
    {
        public readonly DbProviderFactory ProviderFactory;
        public readonly string ConnectionString;
        public readonly StringBuilder Logs = new StringBuilder();
        public bool ThrowOnDbCreationError { get; set; } = false;

        public static volatile bool DisableMemoryOptimizedTables = true;

        public SqlServerSqlInsightsMigrations(DbProviderFactory providerFactory, string connectionString)
        {
            ProviderFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public List<string> GetSqlMigrations()
        {
            // var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(ConnectionString);
            // string dbName = sqlConnectionStringBuilder.InitialCatalog;
            // string serverName = sqlConnectionStringBuilder.DataSource;
            var sqlConnectionStringBuilder = CreateConnectionStringBuilder();
            string dbName = Convert.ToString(sqlConnectionStringBuilder["Initial Catalog"]);
            string serverName = Convert.ToString(sqlConnectionStringBuilder["Data Source"]);

            Logs.AppendLine($" * DB Name: [{dbName}] on server \"{serverName}\"");
            
            IDbConnection cnn = this.ProviderFactory.CreateConnection();
            cnn.ConnectionString = this.ConnectionString;
            var man = cnn.Manage();
            // Logs.AppendLine($" * Is LocalDB: {man.IsLocalDB}");
            Logs.AppendLine($" * SQL Server Version: {man.ProductVersion ?? man.ShortServerVersion} on {man.HostPlatform}");
            Logs.AppendLine($" * Medium Version: {man.MediumServerVersion}");
            Logs.AppendLine($" * Long Version: {man.LongServerVersion}");

			if (man.IsWindows && (man.FixedServerRoles & FixedServerRoles.SysAdmin) != 0)
            {
                // TODO: Only if ISqlInsightsConfiguration.DisposeByShellCommand == true
                man.Configuration.XpCmdShell = true;
                Logs.AppendLine($" * Allow XpCmdShell: {man.Configuration.XpCmdShell}");
            }

            // Server 2016 (13.x) SP1 (or later), any edition. For SQL Server 2014 (12.x) and SQL Server 2016 (13.x) RTM (pre-SP1) you need Enterprise, Developer, or Evaluation edition.
            // 2014 Developer Does not support nvarchar(max) for MOT
            Logs.AppendLine($" * Disable Experimental Memory Optimized Tables: {DisableMemoryOptimizedTables}");
            var supportMOT = man.IsMemoryOptimizedTableSupported && man.ShortServerVersion.Major >= 13;
            Logs.AppendLine($" * Is Memory Optimized Tables Supported: {supportMOT}{(DisableMemoryOptimizedTables & supportMOT ? ", But Disabled": "")}");

            if (DisableMemoryOptimizedTables) supportMOT = false;

            var optimizedCollation = GetOptimizedCollation(cnn);
            Logs.AppendLine($" * Size-Optimized Collation: {(string.IsNullOrEmpty(optimizedCollation) ? ">not supported, using default<" : $"'{optimizedCollation}'")}");
            string legacyKeyPathType = "nvarchar(450)";
            string optimizedKeyPathType = $"varchar(880) Collate {optimizedCollation}";
            var sqlKeyPathType = string.IsNullOrEmpty(optimizedCollation) ? legacyKeyPathType : optimizedKeyPathType;
            var sqlKeyPathTypeMot = supportMOT || string.IsNullOrEmpty(optimizedCollation) ? legacyKeyPathType : optimizedKeyPathType;

            // Disable UTF8 Strings
            sqlKeyPathType = sqlKeyPathTypeMot = legacyKeyPathType;

			// MOT Folder
			var sampleFile = cnn.Query<string>("Select Top 1 filename from sys.sysfiles").FirstOrDefault();
            var dataFolder = sampleFile != null ? CrossPath.GetDirectoryName(man.IsWindows, sampleFile) : null;
            var motFileFolder = CrossPath.Combine(man.IsWindows, dataFolder, $"MOT for {dbName}");
            Logs.AppendLine($" * MOT Files Default Folder: {dataFolder}");

			var existingTables = cnn.Query<string>("Select name from sys.objects WHERE type = 'U' and name like '%SqlInsights%' Order By 1").ToArray();
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

            // SCHEMA_AND_DATA, SCHEMA_ONLY
            string sqlWithMemory = supportMOT ? " WITH (MEMORY_OPTIMIZED=ON, DURABILITY=SCHEMA_AND_DATA)" : "";

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
    Version BigInt Not Null, -- Replaced By @@DBTS
    Guid UniqueIdentifier Not Null, -- Replaced By @@DBTS
    DbInstanceUid UniqueIdentifier Not Null, -- Used By Strings Cache of another cache
    Constraint PK_SqlInsightsKeyPathSummaryTimestamp Primary Key {(supportMOT ? "NON" : "")}Clustered (Guid)
){sqlWithMemory};
If Not Exists(Select Version From SqlInsightsKeyPathSummaryTimestamp)
Insert SqlInsightsKeyPathSummaryTimestamp(Version, Guid, DbInstanceUid) Values(0, NewId(), NewId());
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
    KeyPath {sqlKeyPathTypeMot} Not Null,
    AppName bigint Not Null,
    HostId bigint Not Null,
    IdSession bigint Not Null,
    Version BigInt Not Null,

    -- Data nvarchar(max) Not Null,
    [Count] bigint Not Null,
    ErrorsCount bigint Not Null,
    AppDuration real Not Null,
    AppDurationSquared real Not Null,      -- StdDev ability
    AppKernelUsage real Not Null,
    AppKernelUsageSquared real Not Null,   -- StdDev ability
    AppUserUsage real Not Null,
    AppUserUsageSquared real Not Null,     -- StdDev ability
    SqlDuration bigint Not Null,
    SqlDurationSquared bigint Not Null,    -- StdDev ability
    SqlCPU bigint Not Null,
    SqlCPUSquared bigint Not Null,         -- StdDev ability
    SqlReads bigint Not Null,
    SqlReadsSquared bigint Not Null,       -- StdDev ability
    SqlWrites bigint Not Null,
    SqlWritesSquared bigint Not Null,     -- StdDev ability
    SqlRowCounts bigint Not Null,
    SqlRowCountsSquared bigint Not Null,  -- StdDev ability
    SqlRequests bigint Not Null,
    SqlRequestsSquared bigint Not Null,   -- StdDev ability
    SqlErrors bigint Not Null,
    SqlErrorsSquared bigint Not Null,     -- StdDev ability
    
    {IfLegacy("InternalVersion RowVersion Not Null,")}

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
    KeyPath {sqlKeyPathType} Not Null,
    AppName bigint Not Null,
    HostId bigint Not Null,
    IsOK bit Not Null,

    -- Next 9 columns need for UI sorting only. Just to make sure grouping is properly implemented.
    AppDuration real Not Null,
    AppKernelUsage real Not Null,
    AppUserUsage real Not Null,
    SqlDuration bigint Not Null,
    SqlCPU bigint Not Null,
    SqlReads bigint Not Null,
    SqlWrites bigint Not Null,
    SqlRowCounts bigint Not Null,
    SqlRequests bigint Not Null,
    SqlErrors bigint Not Null,

    Data nvarchar(max) Not Null,
    InternalVersion RowVersion Not Null,
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

/*
-- 1A: Work great WITHOUT filtering details by IsOK.
CREATE NONCLUSTERED INDEX [IX_SqlInsightsAction_IdSession_KeyPath_IdAction_Data]
    ON [dbo].[SqlInsightsAction] ([IdSession],[KeyPath])
    INCLUDE ([IdAction],[Data]);

-- 2B: Next Index and 2 stats RECOMMENDED filtering details 
CREATE NONCLUSTERED INDEX [IX_SqlInsightsAction_KeyPath_IdSession_IsOK_IdAction_Data]
  ON [dbo].[SqlInsightsAction]
           ([KeyPath] ASC, [IdSession] ASC, [IsOK] ASC)
  INCLUDE  
           ([IdAction],[Data]); -- WITH (SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF) ON [PRIMARY]

CREATE STATISTICS [STAT_SqlInsightsAction_IdSession_KeyPath_IsOK] ON [dbo].[SqlInsightsAction]([IdSession], [KeyPath], [IsOK]); -- WITH AUTO_DROP = OFF
CREATE STATISTICS [STAT_SqlInsightsAction_IsOK_KeyPath] ON [dbo].[SqlInsightsAction]([IsOK], [KeyPath]); -- WITH AUTO_DROP = OFF
*/

-- 3C: Final index FOR filtering IsOK, *OR* not filtering IsOK
CREATE NONCLUSTERED INDEX [IX_SqlInsightsAction_IdSession_KeyPath_IsOK_IdAction_Data]
  ON [dbo].[SqlInsightsAction]
           ([IdSession],[KeyPath],[IsOK])
  INCLUDE
           ([IdAction],[Data]);

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

                Logs.AppendLine($" * Database [{GetDatabaseName()}] default collation is '{con.Manage().CurrentDatabase.DefaultCollationName}'");
                Logs.Append($" * Done! Migration successfully invoked {sqlMigrations.Count} commands");
            }
        }

        // public for Tests only
        public string GetOptimizedCollation(IDbConnection cnn)
        {
            // Create Trace fails if Latin1_General_100_BIN2_UTF8 collation
            // But work with Latin1_General_100_CI_AS_SC_UTF8
            // FYI: SQL_Latin1_General_CP1_CI_AS is default collation
            // return null;
            var sqlGetOptimizedCollationName = "Select Top 1 Name From fn_helpcollations() Where Name = 'Latin1_General_100_CI_AS_SC_UTF8'";
            return cnn.ExecuteScalar<string>(sqlGetOptimizedCollationName);
        }

        private string GetDatabaseName()
        {
            // SqlConnectionStringBuilder master = new SqlConnectionStringBuilder(ConnectionString);
            // return master.InitialCatalog;
            var builder = CreateConnectionStringBuilder();
            return Convert.ToString(builder["Initial Catalog"]);
        }

        DbConnectionStringBuilder CreateConnectionStringBuilder()
        {
            DbConnectionStringBuilder ret = this.ProviderFactory.CreateConnectionStringBuilder();
            ret.ConnectionString = ConnectionString;
            return ret;
        }

        // Public for Tests only
        private void CreateDatabaseIfNotExists()
        {
            DbConnectionStringBuilder master = CreateConnectionStringBuilder();
            var dbName = Convert.ToString(master["Initial Catalog"]);
            master.Remove("Initial Catalog");
            if (string.IsNullOrEmpty(dbName)) return; // if dbName is missing it means default db. e.g. already exists
            // SqlConnectionStringBuilder master = new SqlConnectionStringBuilder(ConnectionString);
            // var dbName = master.InitialCatalog;
            // if (string.IsNullOrEmpty(dbName)) return; // if dbName is missing it means default db. e.g. already exists
            // master.InitialCatalog = "";

            try
            {
                var con = ProviderFactory.CreateConnection();
                con.ConnectionString = master.ConnectionString;
                
                try
                {
                    if (con.Manage().IsDbExists(dbName)) return;
                }
                catch
                {
                }

                var optimizedCollation = GetOptimizedCollation(con);
                string sqlCollation = string.IsNullOrEmpty(optimizedCollation) ? "" : $"COLLATE {optimizedCollation}";
                string sqlCommands = @$"
Select DB_ID('{dbName}');
If DB_ID('{dbName}') Is Null 
Begin 
    Create Database [{dbName}] {sqlCollation}; 
    -- The scenario is for development only
    Exec('Alter Database [{dbName}] Set Recovery Simple'); 
End
";

                using (con)
                {
                    con.Execute(sqlCommands, null);
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