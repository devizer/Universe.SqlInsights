& choco feature enable -n allowGlobalConfirmation
& choco feature disable -n showDownloadProgress
choco install git
Add-Folder-To-System-Path "C:\Program Files\Git\bin"

setx PS1_TROUBLE_SHOOT "On"
setx SQLSERVERS_SETUP_FOLDER "C:\SQL-Setup"
setx PS1_REPO_DOWNLOAD_FOLDER "C:\Temp\DevOps"
