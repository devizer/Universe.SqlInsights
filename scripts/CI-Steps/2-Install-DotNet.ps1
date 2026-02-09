Import-DevOps

pushd $ENV:USERPROFILE
Download-File-Managed https://dot.net/v1/dotnet-install.ps1 ".\dotnet-install.ps1"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12;
if ($PSVersionTable.PSEdition -ne "Core") {
  [System.Net.ServicePointManager]::ServerCertificateValidationCallback={$true};
}

foreach($ver in @("6.0", "8.0", "10.0")) {
  . .\dotnet-install.ps1 -Channel "$ver" -InstallDir "C:\Program Files\dotnet"
}

Add-Folder-To-System-Path "C:\Program Files\dotnet"

$tools_folder="$($ENV:USERPROFILE)\.dotnet\tools"
New-item "$tools_folder" -ItemType Directory -Force -EA SilentlyContinue | Out-Null
Add-Folder-To-System-Path "$tools_folder"

dotnet --info

popd