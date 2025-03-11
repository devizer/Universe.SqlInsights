$instance="(local)"
$connectionString="Data Source=$instance; Integrated Security=SSPI;Encrypt=False"
if ("$(ENV:STRESS_CONNECTION_STRING)" -ne "") { $connectionString = "$(ENV:STRESS_CONNECTION_STRING)"; }
Write-Host "Invoke a commands set on instance '$i'"

# 30000 to fit buffer page into 1.4 Gb of RAM
$migrate=@(
 "If Exists(Select 1 From sys.databases where name='DBTest') Drop Database DBTest;",
 "Create Database DBTest;",
 "Use DBTest; Create Table Detail(Id int identity, Description nvarchar(max), Constraint PK_Detail Primary Key (Id));",
 "Use DBTest; EXEC('Create Procedure [Stress By Insert] As Begin Declare @desc char(4000); Set @desc = ''A Description''; Insert Detail(Description) Values(@desc); End')",
 "Use DBTest; EXEC('Create Procedure [Stress By Select] As Begin Select Top 2000 Id, Description From Detail Order By Id Desc; End')"
);
$ins="Use DBTest; Set NoCount On; Declare @i int; Set @i = 0; While @i < 10000 Begin Exec [Stress By Insert]; Set @i = @i + 1; End;";
$select="Use DBTest; Set NoCount On; Declare @i int; Set @i = 0; While @i < 50 Begin Exec [Stress By Select]; Set @i = @i + 1; End;";

$commands=$migrate + @($ins, $select, $ins, $select, $ins, $select, $ins, $select);

foreach($cmd in $commands) { 
  Write-Host "Invoke: $cmd"; 
  Measure-Action "Stress" { Invoke-SqlServer-Command -Title "Instance" -ConnectionString "$connectionString" -SqlCommand $cmd; }
}
