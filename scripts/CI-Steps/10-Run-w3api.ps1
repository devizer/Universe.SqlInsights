. "$PSScriptRoot\Functions.ps1"

cd C:\App\Goods\w3api
$ENV:ASPNETCORE_URLS="http://*:50420"



# Start-Process "dotnet" -ArgumentList "Universe.SqlInsights.W3Api.dll".Split(" ") -NoNewWindow
# & { dotnet Universe.SqlInsights.W3Api.dll } &
$logsFolder = "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\SqlInsights Dashboard Logs"
$ENV:LocalLogsFolder__Windows = "$logsFolder"
$ENV:LocalLogsFolder__Enable = "True"
Smart-Start-Process "dotnet" "Universe.SqlInsights.W3Api.dll"

Sleep 60
Show-Chrome-Program-List

Show-Dotnet-And-Chrome-Processes "ALL is Started"

Show-Chrome-Processes

# SHOW Logs
$logsExists = [bool] (Test-Path $logsFolder)
Say "W3API logsFolder = [$logsFolder]"
Say "W3API logsExists = [$logsExists]"
if ($logsExists) { 
  Get-ChildItem $logsFolder | Format-Table -AutoSize 
  Get-ChildItem -Path $logsFolder -File | ForEach-Object { 
     Say "W3API LOG FILE $($_.FullName)";
     Get-Content "$($_.FullName)" -Raw | out-host 
  }
}

