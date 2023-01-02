$osqlBin="C:\Program Files\Microsoft SQL Server\150\Tools\Binn\OSQL.EXE"
$aw2014Url="https://github.com/Microsoft/sql-server-samples/releases/download/adventureworks/AdventureWorks2014.bak"
$dataDir="$($ENV:SYSTEMDRIVE)\SQL\AdventureWorks2014"

$osqlBin
New-Item -Type Directory $dataDir -ea SilentlyContinue;

$isDownloadCompleted=-Not [String]::IsNullOrWhiteSpace((Get-content -ea SilentlyContinue "$dataDir\AdventureWorks2014.bak.completed"))
Write-Host "AdventureWorks2014.bak is completed: $isDownloadCompleted"
if (-Not $isDownloadCompleted) {
  Write-Host "Downloading [$aw2014Url]"
  & curl.exe -kSLf -o "$dataDir\AdventureWorks2014.bak" $aw2014Url
  if ($LASTEXITCODE -eq 0) {
    echo "complete" >> "$dataDir\AdventureWorks2014.bak.completed"
  }
}

Write-Host "Restoring AdventureWorks2014.bak"
$sqlCommands=@(
  "ALTER DATABASE [AdventureWorks2014] SET SINGLE_USER WITH ROLLBACK IMMEDIATE",
  "RESTORE DATABASE [AdventureWorks2014] FROM DISK = N'$dataDir\AdventureWorks2014.bak' WITH FILE = 1, MOVE N'AdventureWorks2014_Data' TO N'$dataDir\AdventureWorks2014_Data.mdf',  MOVE N'AdventureWorks2014_Log' TO N'$dataDir\AdventureWorks2014_Log.ldf', NOUNLOAD, REPLACE",
  "ALTER DATABASE [AdventureWorks2014] SET MULTI_USER",
  "ALTER DATABASE [AdventureWorks2014] SET RECOVERY SIMPLE"
);
foreach($sqlCmd in $sqlCommands) {
  Write-Host $sqlCmd -ForeGroundColor DarkGreen
  & $osqlBin -S . -E -Q "$sqlCmd"
}

Write-Host "Apply custom stored procedures"
$customSqlFiles=@("SalesGetSalesOrders.sql", "SalesGetSalesOrderDetails.sql");
foreach($customSqlFile in $customSqlFiles) {
  Write-Host "Apply $customSqlFile" -ForeGroundColor DarkYellow
  & $osqlBin -S . -E -i "$customSqlFile"
  Write-Host ""
}
