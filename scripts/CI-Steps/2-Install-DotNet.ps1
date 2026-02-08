foreach($ver in @("6.0", "10.0") {
  Run-Remote-Script https://dot.net/v1/dotnet-install.ps1 -Channel "6.0" -Runtime aspnetcore -InstallDir "C:\Program Files\dotnet"
}

Add-Folder-To-System-Path "C:\Program Files\dotnet"

$tools_folder="$($ENV:USERPROFILE)\.dotnet\tools"
New-item "$tools_folder" -ItemType Directory -Force -EA SilentlyContinue | Out-Null
Add-Folder-To-System-Path "$tools_folder"
