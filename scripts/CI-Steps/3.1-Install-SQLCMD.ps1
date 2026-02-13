Import-DevOps

& choco install sqlserver-cmdlineutils --version 15.0.4298.100
$p="C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn"
Add-Folder-To-User-Path $p
Add-Folder-To-System-Path $p

Say "SHOW SQL Collations"
& sqlcmd -S "(local)" -h-1 -s"," -E -W -w 10000 -Q "SELECT Name, Description FROM fn_helpcollations() order by 1"

Say "SHOW DATABASE FILES"
& sqlcmd -S "(local)" -h-1 -s"|" -E -W -w 10000 -Q "SELECT type_desc, name, physical_name FROM sys.database_files"
