Import-DevOps

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
   foreach($candidate in @("C:\Program Files (x86)\Chromium\Application\chrome.exe", "C:\Program Files\Chromium\Application\chrome.exe", "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", "C:\Program Files\Google\Chrome\Application\chrome.exe")) {
     if (Test-Path $candidate) { $chromePath=$candidate }
   }
   try { $ver = (Get-Item "$chromePath").VersionInfo.ProductVersion; Write-Host "BROWSER VERSION $ver OPENING $url ..." } catch {}
   if (-not ("$chromePath") -or -not (Test-Path $chromePath)) {
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

function Set-Var {
    param (
        [Parameter(Mandatory=$true)] [string]$Name,
        [Parameter(Mandatory=$true)] [string]$Value
    )


    try {
        $prev_value = [Environment]::GetEnvironmentVariable($Name)
        Set-Content -Path "Env:$Name" -Value $Value
        $registryPath = "HKCU:\Environment"
        Set-ItemProperty -Path $registryPath -Name $Name -Value $Value -ErrorAction Stop

        <#
        # Notify the system that environment variables have changed
        # This prevents the need for a reboot/logoff for most apps
        $signature = '[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
                      public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);'
        
        $type = Add-Type -MemberDefinition $signature -Name "NativeMethods" -Namespace "Win32" -PassThru
        
        $result = [UIntPtr]::Zero
        # 0xffff = HWND_BROADCAST, 0x001A = WM_SETTINGCHANGE
        $type::SendMessageTimeout([IntPtr]0xffff, 0x001A, [UIntPtr]::Zero, "Environment", 0x02, 1000, out $result)
        #>

        if ("$prev_value" -ne $Value) {
           Write-Host "Variable '$Name' set to '$Value'." -ForegroundColor Green
        }
    }
    catch {
        Write-Error "Failed to write ENV variable '$Name': $($_.Exception.Message)"
    }
}


Set-Var "PS1_TROUBLE_SHOOT" "On"
Set-Var "SQLSERVERS_SETUP_FOLDER" "C:\SQL-Setup"
Set-Var "PS1_REPO_DOWNLOAD_FOLDER" "C:\Temp\DevOps"
Set-Var "DOTNET_CLI_TELEMETRY_OPTOUT" "1"

Set-Var "SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Title" "CPU"
Set-Var "SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Kind" "String"
Set-Var "SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Value" "$(Get-Cpu-Name)"
Set-Var "SQL_ADMINISTRATIVE_VIEWS_SUMMARY_1_Cpu_Position" "Header"

if ("$($ENV:DB_DATA_DIR)") {
  Set-Var "SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Title" "Data RAM Disk (MB)"
  Set-Var "SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Kind" "Natural"
  Set-Var "SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Value" "$($ENV:RAM_DISK_SIZE)"
  Set-Var "SQL_ADMINISTRATIVE_VIEWS_SUMMARY_RAM_Disk_Position" "9999"
}

Set-Var "TEST_SQL_NET_DURATION_OF_Upload" "42"
Set-Var "TEST_SQL_NET_DURATION_OF_Download" "42"
Set-Var "TEST_SQL_NET_DURATION_OF_Ping" "150"
