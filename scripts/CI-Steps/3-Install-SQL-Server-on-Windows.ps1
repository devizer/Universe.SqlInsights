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

# LocalDB 2019 CU can upgrade without stopping instance
if ("$ENV:SQL" -match "2019 LocalDB") { Update-SqlServer-LocalDB-and-Shared-Tools "2019" }

# LocalDB 2017 CU needs empty instance list
if ("$ENV:SQL" -match "2017 LocalDB") { 
  Mute-RebootRequired-State
  Write-Host "DELETING ALL LocalDB Instances" -ForegroundColor Magenta
  $__ = Find-LocalDb-SqlServers |
       % { "$($_.Instance)" } |
       % { Say "Deleting $_"; Delete-LocalDB-Instance "$_" }
  Update-SqlServer-LocalDB-and-Shared-Tools "2017" 
  $isCreated = Create-LocalDB-Instance "MSSQLLocalDB"
}

# Reset Any Instance to MSSQLLocalDB (actual to v2012 only)
if ("$ENV:SQL" -match "LocalDB") {
  $isDeleted = Delete-LocalDB-Instance "v11.0"
  $isCreated = Create-LocalDB-Instance "MSSQLLocalDB"
}


Write-SQL-Server-Version-Artifacts
