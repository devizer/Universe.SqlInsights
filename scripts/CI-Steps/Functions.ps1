Import-DevOps

setx PS1_TROUBLE_SHOOT "On"
setx SQLSERVERS_SETUP_FOLDER "C:\SQL-Setup"
setx PS1_REPO_DOWNLOAD_FOLDER "C:\Temp\DevOps"

$ENV:PS1_TROUBLE_SHOOT="On"
$ENV:SQLSERVERS_SETUP_FOLDER="C:\SQL-Setup"
$ENV:PS1_REPO_DOWNLOAD_FOLDER="C:\Temp\DevOps"


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

function Get-FreePort { $listener = [System.Net.Sockets.TcpListener]0; $listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop(); return $port }

function Smart-Start-Process([string] $exe, [string] $parameters) {
   $psi = New-Object System.Diagnostics.ProcessStartInfo
   $psi.FileName = $exe
   $psi.Arguments = $parameters
   $psi.UseShellExecute = $true
   $psi.CreateNoWindow = $true
   $psi.WorkingDirectory = "$(Get-Location)"
   # $psi.RedirectStandardOutput = $false 
   # $psi.RedirectStandardError = $false  
   $proc = [System.Diagnostics.Process]::Start($psi)
}

function Open-Url-By-Chrome-On-Windows([string] $url) {
   foreach($candidate in @("C:\Program Files (x86)\Chromium\Application\chrome.exe", "C:\Program Files\Chromium\Application\chrome.exe")) {
     if (Test-Path $candidate) { $chromePath=$candidate }
   }
   try { $ver = (Get-Item "$chromePath").VersionInfo.ProductVersion; Write-Host "BROWSER VERSION $ver OPENING $url ..." } catch {}
   if (-not (Test-Path $chromePath)) {
      Write-Host "[Open-Url-By-Chrome] WARNING! Chromium is missing '$chromePath'" -ForeGroundColor Red;
      return $false
   }

   $chromeArgs = @(
       "--headless",
       "--disable-gpu",
       "--enable-logging",
       "--no-first-run",
       "--no-sandbox",
       "--remote-debugging-port=$(Get-FreePort)",
       "$url"
   )

   # Start-Process $chromePath -ArgumentList $chromeArgs
   Smart-Start-Process $chromePath "$chromeArgs"
}

function Show-Chrome() {
  $chomes = @(Get-Process | Where-Object { $_.ProcessName -match "chrome" })

  if (-not $chomes) {
    Write-Host "Browser Chrome is not running" -ForeGroundColor Red;
  } else {
    $megabytes = (Get-Process chrome | Measure-Object WorkingSet64 -Sum).Sum / 1MB
    $megabytes = [Math]::Round($megabytes,1)
    Say "Total $($chomes.Count) processes are running, total $megabytes MB"
    $chomes | Format-Table -autosize | Out-String -width 123 | Out-Host 
  }
}

function Kill-Chrome() {
  & taskkill /f /t /im chrome.exe 2>$null
}

function show-mem() {
   $memDescription = Get-Memory-Info | ForEach-Object { $_.Description }
   Say "Memory: $memDescription"
   Write-Host "CPU: $(Get-Cpu-Name -includeCoreCount)"
}
