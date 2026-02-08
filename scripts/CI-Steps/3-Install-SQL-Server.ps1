Run-Remote-Script https://raw.githubusercontent.com/devizer/Universe.SqlServerJam/master/SQL-Server-in-Windows-Container/Setup-SQL-Server-in-Container.ps1 *>&1 | Tee-Object -FilePath "C:\App\SQL-DISCOVERY.TXT"

if ("$ENV:SQL" -match "2005") {
  Say "Switch SQL Server 2005 to Local System account"
  & net.exe stop MSSQLSERVER
  & sc.exe config MSSQLSERVER obj= LocalSystem
  & net.exe start MSSQLSERVER
}

