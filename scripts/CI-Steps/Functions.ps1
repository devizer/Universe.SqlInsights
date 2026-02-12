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

function Find-Chrome-Exe() {
   foreach($candidate in @("C:\Program Files (x86)\Chromium\Application\chrome.exe", "C:\Program Files\Chromium\Application\chrome.exe", "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", "C:\Program Files\Google\Chrome\Application\chrome.exe")) {
     if (Test-Path $candidate) { $chromePath=$candidate }
   }

   try { 
      $info = (Get-Item "$chromePath").VersionInfo; 
      $ver = $info.ProductVersion; 
      $product = $info.ProductName; 
      # $info | fl | out-host 
   } catch {}
   $description = "Missing Chrome and Chromium, Not Found"; if ($ver) { $description = "Browser '$product' v$($ver) location is '$chromePath'" }
   return @{ FullPath = $chromePath; Version = $ver; Product = $product; Description = $description} 
}
$chrome = Find-Chrome-Exe;
Write-Line -TextMagenta "$((Find-Chrome-Exe).Description)"
# $chrome | ft -autosize


function Open-Url-By-Chrome-On-Windows([string] $url) {
   $chrome = Find-Chrome-Exe;
   if (-not $chrome.FullPath) {
      Write-Line -TextRed "[Open-Url-By-Chrome] WARNING! Chromium is missing";
      return $false;
   }
   Write-Line -TextCyan "OPENING $url By [$($chrome.Description)] ..."

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
   Smart-Start-Process $chrome.FullPath "$chromeArgs"
}

function Show-Chrome() {
  $chomes = @(Get-Process | Where-Object { $_.ProcessName -match "chrome" })

  if (-not $chomes) {
    Write-Line -TextRed "WARNING! Browser Chrome is not running";
  } else {
    $megabytes = (Get-Process chrome | Measure-Object WorkingSet64 -Sum).Sum / 1MB
    $megabytes = [Math]::Round($megabytes,1)
    Say "Total $($chomes.Count) Chrome Processes are running, total $megabytes MB"
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
   # Write-Line -TextMagenta (Get-Memory-Info).Description
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

        if ("$prev_value" -ne $Value) {
           Write-Line "Env Variable " -TextMagenta "'$Name'" -Reset " set to " -TextGreen "'$Value'"
           # Write-Host "Variable '$Name' set to '$Value'." -ForegroundColor Green
        }
    }
    catch {
        Write-Line -TextRed "Failed to write ENV variable '$Name': $($_.Exception.Message)"
    }
}

Function BroadCast-Variables() {
    try {
        # Сигнатура Win32 API
        $signature = '[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
                      public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);'

        # Добавляем тип (с проверкой, чтобы избежать ошибок при повторном вызове)
        if (-not ([System.Management.Automation.PSTypeName]"Win32.NativeMethods").Type) {
            Add-Type -MemberDefinition $signature -Name "NativeMethods" -Namespace "Win32"
        }

        $result = [UIntPtr]::Zero
        # Используем [ref] вместо out
        $ret = [Win32.NativeMethods]::SendMessageTimeout([IntPtr]0xffff, 0x001A, [UIntPtr]::Zero, "Environment", 0x02, 1000, [ref]$result)
        
        Write-Line -TextGreen "Broadcast variables success, SendMessageTimeout() --> $ret, [ref] result = $result"
    } 
    catch {
        Write-Line -TextRed "Broadcast variables failed: $($_.Exception.Message)"
    }
}
# BroadCast-Variables
# OK: Bradcast variables success, SendMessageTimeout() --> 1, [ref] result = 0

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
BroadCast-Variables
