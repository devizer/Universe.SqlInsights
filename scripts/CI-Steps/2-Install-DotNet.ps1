Import-DevOps

# pushd "$ENV:USERPROFILE"

<#
Download-File-Managed https://dot.net/v1/dotnet-install.ps1 ".\dotnet-install.ps1"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12;
if ($PSVersionTable.PSEdition -ne "Core") {
  [System.Net.ServicePointManager]::ServerCertificateValidationCallback={$true};
}
#>

# 2016 by default TLS is not 1.2
foreach($ver in @("6.0", "8.0", "10.0")) {
  Run-Remote-Script https://dot.net/v1/dotnet-install.ps1 -Channel "$ver" -InstallDir "C:\Program Files\dotnet"
}

Add-Folder-To-System-Path "C:\Program Files\dotnet"

$tools_folder="$($ENV:USERPROFILE)\.dotnet\tools"
New-item "$tools_folder" -ItemType Directory -Force -EA SilentlyContinue | Out-Null
Add-Folder-To-User-Path "$tools_folder"

& "C:\Program Files\dotnet\dotnet" --info

Say "Installing dotnet serve ..."
& "C:\Program Files\dotnet\dotnet" tool install --global dotnet-serve


# popd
