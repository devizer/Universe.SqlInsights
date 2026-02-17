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
foreach($ver in @("6.0", "8.0", "10.0")) {
  If ((Get-OS-Platform) -eq "Windows") {
     Run-Remote-Script "https://dot.net/v1/dotnet-install.$ext" -Channel "$ver" -InstallDir "C:\Program Files\dotnet"
  } Else {
     $ENV:DOTNET_VERSIONS="$ver"
     $ENV:SKIP_DOTNET_DEPENDENCIES="True"
     Run-Remote-Script https://raw.githubusercontent.com/devizer/test-and-build/master/lab/install-DOTNET.sh
  }
}


If ((Get-OS-Platform) -eq "Windows") {
   Add-Folder-To-System-Path "C:\Program Files\dotnet"
   $tools_folder="$($ENV:USERPROFILE)\.dotnet\tools"
   New-item "$tools_folder" -ItemType Directory -Force -EA SilentlyContinue | Out-Null
   Add-Folder-To-User-Path "$tools_folder"
   & "C:\Program Files\dotnet\dotnet" --info
   Say "Installing dotnet serve (windows) ..."
   & "C:\Program Files\dotnet\dotnet" tool install --global dotnet-serve
} Else {
   Say "Installing dotnet serve (linux) ..."
   & dotnet tool install --global dotnet-serve
}
