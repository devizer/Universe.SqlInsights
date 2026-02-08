& choco install sqlserver-cmdlineutils --version 15.0.4298.100
Add-Folder-To-User-Path "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn"
Add-Folder-To-System-Path "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn"

Say "SHOW SQL Collations"
& sqlcmd -S "(local)" -h-1 -s"," -E -W -w 10000 -Q "SELECT Name, Description FROM fn_helpcollations() order by 1"
