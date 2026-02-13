. "$PSScriptRoot\Functions.ps1"

Show-Dotnet-And-Chrome-Processes "BEFORE"

foreach($name in "chrome.exe", "dotnet.exe", "dotnet-serve.exe") {
  Say "KILL $name"
  & taskkill /f /t /im chrome.exe "$name"
}

Show-Dotnet-And-Chrome-Processes "AFTER"
