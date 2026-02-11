. "$PSScriptRoot\Functions.ps1"

echo "`$PSNativeCommandArgumentPassing = [$PSNativeCommandArgumentPassing]"
& choco feature enable -n allowGlobalConfirmation
& choco feature disable -n showDownloadProgress
choco install git
Add-Folder-To-System-Path "C:\Program Files\Git\bin"


