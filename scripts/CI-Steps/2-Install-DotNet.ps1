. "$PSScriptRoot\Functions.ps1"
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
$ext=If ((Get-OS-Platform) -eq "Windows") { "ps1" } Else { "sh" }
$target_for_windows = if (Test-Path "D:\") { "D:\dotnet" } Else { "C:\Program Files\dotnet" }
foreach($ver in @("6.0", "8.0", "10.0")) {
  If ((Get-OS-Platform) -eq "Windows") {
     Write-Host "TEMP = [$($ENV:TEMP)]"
     Run-Remote-Script "https://dot.net/v1/dotnet-install.$ext" -Channel "$ver" -InstallDir "$target_for_windows"
  } Else {
     $ENV:DOTNET_VERSIONS="$ver"
     $ENV:SKIP_DOTNET_DEPENDENCIES="True"
     Run-Remote-Script https://raw.githubusercontent.com/devizer/test-and-build/master/lab/install-DOTNET.sh
     & sudo ln -f -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet
     Show-Last-Exit-Code "Create link at /usr/local/bin/dotnet" -Throw
  }
}


If ((Get-OS-Platform) -eq "Windows") {
   Add-Folder-To-System-Path "$target_for_windows"
   if ((Is-AZURE_PIPELINE)) { Write-Host "##vso[task.prependpath]$target_for_windows" }
   if ((Is-GITHUB-ACTIONS) -and ("$env:GITHUB_PATH" -ne "")) { "$target_for_windows" | Out-File -FilePath $env:GITHUB_PATH -Encoding utf8 -Append }
   $tools_folder="$($ENV:USERPROFILE)\.dotnet\tools"
   New-item "$tools_folder" -ItemType Directory -Force -EA SilentlyContinue | Out-Null
   Add-Folder-To-User-Path "$tools_folder"
   & "$target_for_windows\dotnet" --info
   Say "Installing dotnet serve (windows) ..."
   & "$target_for_windows\dotnet" tool install --global dotnet-serve
} Else {
   Say "Installing dotnet serve (linux) ..."
   & dotnet tool install --global dotnet-serve
}
