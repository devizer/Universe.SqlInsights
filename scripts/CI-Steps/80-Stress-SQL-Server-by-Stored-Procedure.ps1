. "$PSScriptRoot\Functions.ps1"

$instance="(local)"
$connectionString="Data Source=$instance; Integrated Security=SSPI;Encrypt=False"
if ("$($ENV:STRESS_CONNECTION_STRING)" -ne "") { $connectionString = "$($ENV:STRESS_CONNECTION_STRING)"; }
Write-Host "Invoke a commands set on instance '$i'"
Say "SQL Server for Stress: [$connectionString]"

# 30000 to fit buffer page into 1.4 Gb of RAM
$migrate=@(
 "If Exists(Select 1 From sys.databases where name='DB-Stress') Drop Database [DB-Stress];",
 "Create Database [DB-Stress];",
 "Use [DB-Stress]; Create Table Detail(Id int identity, Description nvarchar(max), Constraint PK_Detail Primary Key (Id));",
 "Use [DB-Stress]; EXEC('Create Procedure [Stress By Insert] As Begin Declare @desc char(4000); Set @desc = ''A Description''; Insert Detail(Description) Values(@desc); End')",
 "Use [DB-Stress]; EXEC('Create Procedure [Stress By Select] As Begin Select Top 200 Id, Description From Detail Order By Id Desc; End')",
 "Use [DB-Stress]; EXEC('Create Procedure [Stress By Delete] As Begin Delete From Detail Where Id in (Select Top 2 d2.Id From Detail d2 Order By Id Desc); End')"
);
$insert="Use [DB-Stress]; Set NoCount On; Declare @i int; Set @i = 0; While @i < 10000 Begin Exec [Stress By Insert]; Set @i = @i + 1; End;";
$select="Use [DB-Stress]; Set NoCount On; Declare @i int; Set @i = 0; While @i < 50    Begin Exec [Stress By Select]; Set @i = @i + 1; End;";
$delete="Use [DB-Stress]; Set NoCount On; Declare @i int; Set @i = 0; While @i < 1000  Begin Exec [Stress By Delete]; Set @i = @i + 1; End;";

# WITH Delete
  $commands=$migrate + @($insert, $select, $delete, $insert, $select, $insert, $select, $insert, $delete, $select);
# $commands=$migrate + @($insert, $select, $insert, $select, $insert, $select);

foreach($cmd in $commands) { 
  Say "Invoke: «$cmd»"; 
  Measure-Action "Stress" { Invoke-SqlServer-Command -Title "Instance" -ConnectionString "$connectionString" -SqlCommand $cmd; }
}

Say "Bye"

