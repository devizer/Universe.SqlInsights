. "$PSScriptRoot\Functions.ps1"

Show-Dotnet-And-Chrome-Processes "BEFORE"

foreach($name in "chrome.exe", "dotnet.exe", "dotnet-serve.exe") {
  Say "KILL $name"
  & taskkill /f /t /im "$name"
}
$Global:LASTEXITCODE = 0

Show-Dotnet-And-Chrome-Processes "AFTER"
