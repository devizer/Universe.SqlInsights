. "$PSScriptRoot\Functions.ps1"

& choco feature enable -n allowGlobalConfirmation
& choco feature disable -n showDownloadProgress
choco install git
Add-Folder-To-System-Path "C:\Program Files\Git\bin"

Say "About BASH"
try { & bash --version } catch {}
try { & bash -c 'uname -a; echo $BASH_VERSION; echo Path is below; echo $PATH' } catch {}
