. "$PSScriptRoot\Functions.ps1"

Show-Dotnet-And-Chrome-Processes "BEFORE"

& taskkill /f /t /im chrome.exe
& taskkill /f /t /im dotnet.exe

Show-Dotnet-And-Chrome-Processes "AFTER"
