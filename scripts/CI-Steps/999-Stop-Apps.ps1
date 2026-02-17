. "$PSScriptRoot\Functions.ps1"

Show-Dotnet-And-Chrome-Processes "BEFORE"

if ((Get-Os-Platform) -ne "Windows") { 
  Say "KILL dotnet"
  & pkill dotnet
  Say "KILL dotnet-serve"
  & pkill dotnet-serve
} Else {
  foreach($name in "chrome.exe", "dotnet.exe", "dotnet-serve.exe") {
    Say "KILL $name"
    & taskkill /f /t /im "$name"
  }
}
$Global:LASTEXITCODE = 0

Show-Dotnet-And-Chrome-Processes "AFTER"
