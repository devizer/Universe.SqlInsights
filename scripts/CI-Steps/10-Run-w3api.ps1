. "$PSScriptRoot\Functions.ps1"

cd C:\App\Goods\w3api
$ENV:ASPNETCORE_URLS="http://*:50420"
# Start-Process "dotnet" -ArgumentList "Universe.SqlInsights.W3Api.dll".Split(" ") -NoNewWindow
# & { dotnet Universe.SqlInsights.W3Api.dll } &
# $ENV:LocalLogsFolder__Windows = "C:\Artifacts\Api-Logs"
# $ENV:LocalLogsFolder__Enable = "True"
Smart-Start-Process "dotnet" "Universe.SqlInsights.W3Api.dll"

Sleep 60
Show-Chrome

Select-WMI-Objects Win32_Process | Select-Object ProcessId, Name, @{Name="WS(MB)"; Expression={[math]::Round($_.WorkingSetSize / 1MB, 1)}}, CommandLine | Sort-Object Name | ft -AutoSize | Out-String -width 200

# SHOW Logs
$logsFolder = "$($ENV:SystemDrive)\\Temp\\SqlInsights Dashboard Logs"
$logsExists = [bool] (Test-Path $logsFolder)
Say "W3API logsFolder = [$logsFolder]"
Say "W3API logsExists = [$logsExists]"
if ($logsExists) { 
  Get-ChildItem $logsFolder | Format-Table -AutoSize 
  Get-ChildItem -Path $logsFolder -File | ForEach-Object { 
     Say "W3API LOG FILE $($_.FullName)";
     $copyTo="$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\SqlInsights Dashboard Logs"
     Write-Host "Copy it to [$copyTo]"
     New-item "$copyTo" -ItemType Directory -Force -EA SilentlyContinue | Out-Null
     Copy-Item -Path "$($_.FullName)" -Destination $copyTo -Force
     Get-Content "$($_.FullName)" -Raw | out-host 
  }
}

Show-Chrome
