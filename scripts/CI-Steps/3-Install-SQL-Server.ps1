. "$PSScriptRoot\Functions.ps1"

Say "Hiding LocalDB Servers"
Remove-Item -Path "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions" -Recurse -Force -EA SilentlyContinue

Run-Remote-Script https://raw.githubusercontent.com/devizer/Universe.SqlServerJam/master/SQL-Server-in-Windows-Container/Setup-SQL-Server-in-Container.ps1 *>&1 | Tee-Object -FilePath "C:\App\SETUP-SQL-SERVER-OUTPUT.TXT"

if ("$ENV:SQL" -match "2005") {
  Say "Switch SQL Server 2005 to Local System account"
  & net.exe stop MSSQLSERVER
  & sc.exe config MSSQLSERVER obj= LocalSystem
  & net.exe start MSSQLSERVER
}

if ("$ENV:SQL" -match "2017 LocalDB") { Update-SqlServer-LocalDB-and-Shared-Tools "2017" }
if ("$ENV:SQL" -match "2019 LocalDB") { Update-SqlServer-LocalDB-and-Shared-Tools "2019" }

if ("$ENV:SQL" -match "LocalDB") {
  $isDeleted = Delete-LocalDB-Instance "v11.0"
  $isCreated = Create-LocalDB-Instance "MSSQLLocalDB"
}

echo "Query SQL Server '$ENV:SQL_INSTANCE_NAME' Medium Version"
try { 
  $sql_ver = Query-SqlServer-Version -Title "Instance $ENV:SQL_INSTANCE_NAME" -Instance "$ENV:SQL_INSTANCE_NAME"
  if ($sql_ver) { 
    Write-Line -TextGreen "Query-SqlServer-Version for Medium Version SUCCESS: $sql_ver"
    Write-Artifact-Info "SQL-SERVER-MEDIUM-VERSION.TXT" "$sql_ver" 
  }
}
catch {}

echo "Query SQL Server '$ENV:SQL_INSTANCE_NAME' Title"
try { 
  $sql_ver = Query-SqlServer-Version -Title "Instance $ENV:SQL_INSTANCE_NAME" -Instance "$ENV:SQL_INSTANCE_NAME" -Kind "Title"
  if ($sql_ver) {
    Write-Line -TextGreen "Query-SqlServer-Version for Title SUCCESS: $sql_ver"
    Write-Artifact-Info "SQL-SERVER-TITLE.TXT" "$sql_ver"
  }
}
catch {}
