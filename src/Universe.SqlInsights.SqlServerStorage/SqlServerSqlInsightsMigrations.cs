﻿using System;
using System.Data.Common;
using System.Data.SqlClient;
using Dapper;

namespace Universe.SqlInsights.SqlServerStorage
{
    public class SqlServerSqlInsightsMigrations
    {
        public readonly DbProviderFactory ProviderFactory;
        public readonly string ConnectionString;
        public bool ThrowOnDbCreationError { get; set; } = false;

        public SqlServerSqlInsightsMigrations(DbProviderFactory providerFactory, string connectionString)
        {
            ProviderFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public static readonly string[] SqlMigrations = new[]
        {
            @"
If Object_ID('SqlInsightsString') Is Null
Create Table SqlInsightsString(
    IdString bigint Identity Not Null,
    Kind tinyint Not Null, -- 1: KeyPath, 2: AppName, 3: HostId
    StartsWith nvarchar(450) Not Null,
    Tail nvarchar(max) Null,
    Constraint PK_SqlInsightsString Primary Key (IdString)
)
",
            
            // This is workaround for memory optimized SqlInsightsKeyPathSummary 
            @"
If Object_ID('SqlInsightsKeyPathSummaryTimestamp') Is Null
Create Table SqlInsightsKeyPathSummaryTimestamp(
    Version BigInt Not Null,
    Guid UniqueIdentifier Not Null,
    Constraint PK_SqlInsightsKeyPathSummaryTimestamp Primary Key (Guid)
);
Insert SqlInsightsKeyPathSummaryTimestamp(Version, Guid) Values(0, NewId())",
            
            @"
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
)
SET IDENTITY_INSERT SqlInsightsSession ON;
Insert SqlInsightsSession(IdSession, StartedAt, IsFinished, Caption) Values(
    0,
    GetUtcDate(),
    (0),
    'Lifetime session'
)    
SET IDENTITY_INSERT SqlInsightsSession OFF;
End
",
            @"
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
)
Create Index IX_SqlInsightsKeyPathSummary_Version On SqlInsightsKeyPathSummary(Version Desc)
End
",
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
    Constraint PK_SqlInsightsAction Primary Key (IdAction),
    Constraint FK_SqlInsightsAction_SqlInsightsSession FOREIGN KEY (IdSession) REFERENCES SqlInsightsSession(IdSession)
        , -- ON DELETE CASCADE ON UPDATE NO ACTION,  -- Used for debugging only, not necessary in runtime
    Constraint FK_SqlInsightsAction_SqlInsightsKeyPathSummary FOREIGN KEY (KeyPath, IdSession, AppName, HostId) REFERENCES SqlInsightsKeyPathSummary(KeyPath, IdSession, AppName, HostId),
        -- ON DELETE CASCADE ON UPDATE NO ACTION   -- Used for debugging only, not necessary in runtime
    Constraint FK_SqlInsightsAction_AppName FOREIGN KEY (AppName) REFERENCES SqlInsightsString(IdString), 
    Constraint FK_SqlInsightsAction_HostId FOREIGN KEY (HostId) REFERENCES SqlInsightsString(IdString), 
)
Create Index IX_SqlInsightsAction_KeyPath_At On SqlInsightsAction(KeyPath, At)
End 
"
        };

        public void Migrate()
        {
            CreateDatabaseIfNotExists();

            // Object_ID Returns null in case of missed permissions    
            using (var con = new SqlConnection(ConnectionString))
            {
                foreach (var sqlMigration in SqlMigrations)
                {
                    try
                    {
                        con.Execute(sqlMigration, null);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Migration failed{Environment.NewLine}{sqlMigration}", ex);
                    }
                }
            }
        }

        private void CreateDatabaseIfNotExists()
        {
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