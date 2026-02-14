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

# Medium Version
try { 
  $sql_ver = Query-SqlServer-Version -Title "Default Instance" -Instance "(local)"
  if ($sql_ver) { 
    Write-Line -TextGreen "Query-SqlServer-Version for Medium Version SUCCESS: $sql_ver"
    Write-Artifact-Info "SQL-SERVER-MEDIUM-VERSION.TXT" "$sql_ver" 
  }
}
catch {}

# Title
try { 
  $sql_ver = Query-SqlServer-Version -Title "Default Instance" -Instance "(local)" -Kind "Title"
  if ($sql_ver) {
    Write-Line -TextGreen "Query-SqlServer-Version for Title SUCCESS: $sql_ver"
    Write-Artifact-Info "SQL-SERVER-TITLE.TXT" "$sql_ver"
  }
}
catch {}
