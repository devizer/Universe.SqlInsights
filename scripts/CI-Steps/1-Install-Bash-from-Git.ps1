echo "`$PSNativeCommandArgumentPassing = [$PSNativeCommandArgumentPassing]"
& choco feature enable -n allowGlobalConfirmation
& choco feature disable -n showDownloadProgress
choco install git
Add-Folder-To-System-Path "C:\Program Files\Git\bin"

setx PS1_TROUBLE_SHOOT "On"
setx SQLSERVERS_SETUP_FOLDER "C:\SQL-Setup"
setx PS1_REPO_DOWNLOAD_FOLDER "C:\Temp\DevOps"
setx DOTNET_CLI_TELEMETRY_OPTOUT "1"

      $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Title="CPU"
      $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Kind="String"
      $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Value="$(Get-Cpu-Name)"
      $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Position="Header"
      setx SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Title "CPU"
      setx SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Kind "String"
      setx SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Value "$(Get-Cpu-Name)"
      setx SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Position "Header"
      if ("$($ENV:DB_DATA_DIR)") {
        $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Title="Data RAM Disk (MB)"
        $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Kind="Natural"
        $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Value="$($ENV:RAM_DISK_SIZE)"
        $ENV:SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Position="9999"
        setx SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Title="Data RAM Disk (MB)"
        setx SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Kind="Natural"
        setx SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Value="$($ENV:RAM_DISK_SIZE)"
        setx SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Position="9999"
      }

setx 