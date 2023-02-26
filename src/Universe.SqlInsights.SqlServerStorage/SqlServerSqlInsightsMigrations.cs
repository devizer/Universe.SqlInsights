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
        public bool DisableMemoryOptimizedTables { get; set; } = false;

        public SqlServerSqlInsightsMigrations(DbProviderFactory providerFactory, string connectionString)
        {
            ProviderFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public List<string> GetSqlMigrations()
        {
            string dbName = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
            Logs.AppendLine($"DisableMemoryOptimizedTables: {DisableMemoryOptimizedTables}");
            Logs.AppendLine($"Db Name: [{dbName}]");
            
            IDbConnection cnn = this.ProviderFactory.CreateConnection();
            cnn.ConnectionString = this.ConnectionString;
            var man = cnn.Manage();
            Logs.AppendLine($"IsLocalDB: {man.IsLocalDB}");
            Logs.AppendLine($"Version: {man.ShortServerVersion}, {man.EngineEdition}");
            var supportMOT = !man.IsLocalDB && man.ShortServerVersion.Major >= 12;
            Logs.AppendLine($"Support MOT: {supportMOT}{(DisableMemoryOptimizedTables & supportMOT ? ", But Disabled": "")}");
            
            // MOT Folder
            var sampleFile = cnn.Query<string>("Select Top 1 filename from sys.sysfiles").FirstOrDefault();
            var dataFolder = sampleFile != null ? Path.GetDirectoryName(sampleFile) : null;
            var motFileFolder = Path.Combine(dataFolder, $"MOT for {dbName}");
            Logs.AppendLine($"MOT Files Folder: {dataFolder}");

            var existingTables = cnn.Query<string>("Select name from SYSOBJECTS WHERE xtype = 'U' and name like '%SqlInsights%'").ToArray();
            // Logs.AppendLine($"Existing Tables: {}");

            string motFileGroup = null;
            if (supportMOT)
            {
                motFileGroup = cnn.Query<string>("Select Top 1 name from sys.filegroups where type = 'FX'").FirstOrDefault();
            }
            bool isMotFileGroupExists = !string.IsNullOrEmpty(motFileGroup);
            Logs.AppendLine($"Existing MOT File Group Name: {(isMotFileGroupExists ? $"'{motFileGroup}'" : ">Not Found<")}");
            
            List<string> sqlMotList = new List<string>();
            if (supportMOT && !isMotFileGroupExists)
            {
                string sqlAutoCloseOff = @$"
ALTER DATABASE [{dbName}] 
SET AUTO_CLOSE OFF;";
                
                string sqlAddMotFileGroup = @$"
ALTER DATABASE [{dbName}]
ADD FILEGROUP MemoryOptimizedTables
CONTAINS MEMORY_OPTIMIZED_DATA;";

                string sqlAddMotFile = @$"
ALTER DATABASE [{dbName}] ADD FILE (
    name='SqlInsight MemoryOptimizedTables', filename='{motFileFolder}')
TO FILEGROUP MemoryOptimizedTables;";

                string sqlEnableTransactions = $@"
ALTER DATABASE [{dbName}]
SET MEMORY_OPTIMIZED_ELEVATE_TO_SNAPSHOT = ON;
";
                sqlMotList.AddRange(new[] { sqlAutoCloseOff, sqlAddMotFileGroup, sqlAddMotFile, sqlEnableTransactions});
            }
            
            string sqlWith = supportMOT ? " WITH (MEMORY_OPTIMIZED=ON, DURABILITY=SCHEMA_ONLY)" : "";
            
            List<string> sqlCreateList = new List<string>
            {
                // TODO: Add Tests for LONG app name or host name
                // Table SqlInsights String
                @$"
If Object_ID('SqlInsightsString') Is Null
BEGIN
Create Table SqlInsightsString(
    IdString bigint Identity Not Null,
    Kind tinyint Not Null, -- 1: KeyPath, 2: AppName, 3: HostId
    StartsWith nvarchar({StringsStorage.MaxStartLength}) Not Null,
    Tail nvarchar(max) Null
    -- Constraint PK_SqlInsightsString Primary Key (Kind, StartsWith) -- TODO: BUG
);
ALTER TABLE SqlInsightsString Add Constraint PK_SqlInsightsString Primary Key NONCLUSTERED (IdString);
If Not Exists (Select 1 From sys.indexes Where name='UCX_SqlInsightsString_Kind_StartsWith')
Create Unique CLUSTERED Index UCX_SqlInsightsString_Kind_StartsWith On SqlInsightsString(Kind, StartsWith);
END
",

                // Table SqlInsights KeyPathSummaryTimestamp
                // This is workaround for memory optimized SqlInsightsKeyPathSummary 
                @$"
If Object_ID('SqlInsightsKeyPathSummaryTimestamp') Is Null
Create Table SqlInsightsKeyPathSummaryTimestamp(
    Version BigInt Not Null,
    Guid UniqueIdentifier Not Null,
    Constraint PK_SqlInsightsKeyPathSummaryTimestamp Primary Key (Guid)
);
If Not Exists(Select Version From SqlInsightsKeyPathSummaryTimestamp)
Insert SqlInsightsKeyPathSummaryTimestamp(Version, Guid) Values(0, NewId())",

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
    Constraint PK_SqlInsightsSession Primary Key (IdSession)
);
SET IDENTITY_INSERT SqlInsightsSession ON;
Insert SqlInsightsSession(IdSession, StartedAt, IsFinished, Caption) Values(
    0,
    GetUtcDate(),
    (0),
    'Lifetime session'
);    
SET IDENTITY_INSERT SqlInsightsSession OFF;
End
",

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
    Constraint PK_SqlInsightsKeyPathSummary Primary Key (KeyPath, IdSession, AppName, HostId),
    Constraint FK_SqlInsightsKeyPathSummary_SqlInsightsSession FOREIGN KEY (IdSession) REFERENCES SqlInsightsSession(IdSession),
    Constraint FK_SqlInsightsKeyPathSummary_AppName FOREIGN KEY (AppName) REFERENCES SqlInsightsString(IdString),
    Constraint FK_SqlInsightsKeyPathSummary_HostId FOREIGN KEY (HostId) REFERENCES SqlInsightsString(IdString)
);
Create Index IX_SqlInsightsKeyPathSummary_Version On SqlInsightsKeyPathSummary(Version Desc)
End
",

                // Table SqlInsights Action
                @"
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
    Constraint PK_SqlInsightsAction Primary Key (KeyPath, IdSession, IdAction),
    Constraint FK_SqlInsightsAction_SqlInsightsSession FOREIGN KEY (IdSession) REFERENCES SqlInsightsSession(IdSession)
        , -- ON DELETE CASCADE ON UPDATE NO ACTION,  -- Used for debugging only, not necessary in runtime
    Constraint FK_SqlInsightsAction_SqlInsightsKeyPathSummary FOREIGN KEY (KeyPath, IdSession, AppName, HostId) REFERENCES SqlInsightsKeyPathSummary(KeyPath, IdSession, AppName, HostId),
        -- ON DELETE CASCADE ON UPDATE NO ACTION   -- Used for debugging only, not necessary in runtime
    Constraint FK_SqlInsightsAction_AppName FOREIGN KEY (AppName) REFERENCES SqlInsightsString(IdString), 
    Constraint FK_SqlInsightsAction_HostId FOREIGN KEY (HostId) REFERENCES SqlInsightsString(IdString), 
);
-- Create Index IX_SqlInsightsAction_KeyPath_IdSession_IdAction On SqlInsightsAction(KeyPath, IdSession, IdAction);
-- Create Index IX_SqlInsightsAction_KeyPath_At On SqlInsightsAction(KeyPath, At);
-- Create Index IX_SqlInsightsAction_KeyPath On SqlInsightsAction(KeyPath);
End 
"
            };

            List<string> ret = new List<string>();
            ret.AddRange(sqlMotList);
            ret.AddRange(sqlCreateList);
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

                Logs.Append($"Migration succesfully invoked {sqlMigrations.Count} commands");
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
If DB_ID('{dbName}') Is Null 
Begin 
    Create Database [{dbName}]; 
    -- The scenario is for development only
    exec('Alter Database [{dbName}] Set Recovery Simple'); 
End";

            try
            {
                var con = ProviderFactory.CreateConnection();
                con.ConnectionString = master.ConnectionString;
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