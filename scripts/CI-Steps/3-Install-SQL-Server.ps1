Import-DevOps

setx PS1_TROUBLE_SHOOT "On"
setx SQLSERVERS_SETUP_FOLDER "C:\SQL-Setup"
$ENV:PS1_TROUBLE_SHOOT="On"
$ENV:SQLSERVERS_SETUP_FOLDER="C:\SQL-Setup"

Run-Remote-Script https://raw.githubusercontent.com/devizer/Universe.SqlServerJam/master/SQL-Server-in-Windows-Container/Setup-SQL-Server-in-Container.ps1 *>&1 | Tee-Object -FilePath "C:\App\SETUP-SQL-SERVER-OUTPUT.TXT"

if ("$ENV:SQL" -match "2005") {
  Say "Switch SQL Server 2005 to Local System account"
  & net.exe stop MSSQLSERVER
  & sc.exe config MSSQLSERVER obj= LocalSystem
  & net.exe start MSSQLSERVER
}

