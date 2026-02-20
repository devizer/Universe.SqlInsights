# Write-Host "[DEBUG] Starting 'Functions.ps1'"
$ErrorActionPreference = "Stop"
Import-DevOps



function Is-GITHUB-ACTIONS() { $ENV:GITHUB_ACTIONS -eq "true" }
function Is-AZURE_PIPELINE() { $ENV:TF_BUILD -eq "true" }

function Get-FreePort { $listener = [System.Net.Sockets.TcpListener]0; $listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop(); return $port }

function Mute-RebootRequired-State() {
  if ((Get-OS-Platform) -ne "Windows") { return; }
  Write-Line -TextCyan "[Mute-RebootRequired-State] Starting ..."
        # 1. Component Based Servicing
  $__ = Remove-Item -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending" -Recurse -Force -ErrorAction SilentlyContinue
        # 2. Windows Update RebootRequired
  $__ = Remove-Item -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired" -Recurse -Force -ErrorAction SilentlyContinue
        # 3. PendingFileRenameOperations
  $__ = Remove-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager" -Name "PendingFileRenameOperations" -Force -ErrorAction SilentlyContinue
  Write-Line -TextCyan "[Mute-RebootRequired-State] Completed"
}

function Show-Last-Exit-Code([string] $Title) {
  if ($Global:LASTEXITCODE) {
    $msg="ERROR! '$Title' failed. Exit Code $($Global:LASTEXITCODE)"
    Say $msg
    Write-Line -TextRed $msg
  } Else {
    $msg="SUCCESS! '$Title' successfully completed wouthout any errors"
    Say $msg
    Write-Line -TextGreen $msg
  }

}


function Smart-Start-Process([string] $exe, [string] $parameters, [int] $guard_timeout = 1500) {
   $psi = New-Object System.Diagnostics.ProcessStartInfo
   $psi.FileName = $exe
   $psi.Arguments = $parameters
   $psi.UseShellExecute = $true
   $psi.CreateNoWindow = $true
   $psi.WorkingDirectory = "$(Get-Location)"
   # $psi.RedirectStandardOutput = $false 
   # $psi.RedirectStandardError = $false  
   $proc = [System.Diagnostics.Process]::Start($psi)
   
   $finished = $proc.WaitForExit($guard_timeout)
   if ($finished) {
       $exitCode = $proc.ExitCode
       if ($exitCode -ne 0) {
           $msg = "`"$exe`" $parameters failed with exit code '$exitCode'"
           Write-Line -TextRed $msg
           throw $msg
       } Else {
           $msg = "SUCCESS: `"$exe`" $parameters successfully completed"
           Write-Line -TextGreen $msg
       }
   }
   # return $proc
}

function Find-Chrome-Program-List() {
   $ret = @()
   # Any OS, but search in the PATH only
   foreach($cmd_name in @("chromium", "google-chrome", "firefox")) { 
     $commands = @(Get-Command "$cmd_name" -CommandType Application -EA SilentlyContinue)
     foreach($cmd in $commands) {
        $cmd_source = "$($cmd.Source)"
        $raw_version_output=$(& "$cmd_source" --version)
        # Write-Host "[Debug] Version for '"$cmd_source"': [$raw_version_output]"
        $raw_version = $null;
        if ($raw_version_output -match '\d+\.\d+\.\d+') {
            $raw_version = $matches[0]
            # Write-Host "[Debug]      raw_version = [$raw_version]"
            if ($raw_version_output -match '^(.*?)\s*(\d+\.\d+\.\d+)') {
                $raw_product_name = $matches[1].Trim()
                # $version = $matches[2]
            }
        }
        $ver = $raw_version
        $product = $raw_product_name
        $description = "Browser '$product' v$($ver) location is '$cmd_source'"
        if (($ver) -and ($product)) {
            $ret += [pscustomobject] @{ FullPath = $cmd_source; Version = $ver; Product = $product; Description = $description}
        }
     }
   }
 
   # Windows Only, search by hardcoded location
   $candidates = @()
   $exe_list = @("Google\Chrome\Application\chrome.exe", "Chromium\Application\chrome.exe")
   foreach($pf in Find-ProgramFiles-Folders) { foreach ($exe_part in $exe_list) {
     $exe = Combine-Path $pf $exe_part;
     if (Test-Path $exe) { $candidates += $exe }
   }}
   # $candidates = @("C:\Program Files (x86)\Chromium\Application\chrome.exe", "C:\Program Files\Chromium\Application\chrome.exe", "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", "C:\Program Files\Google\Chrome\Application\chrome.exe")
   foreach($candidate in $candidates) {
     if (Test-Path $candidate) { 
         try { 
            $info = (Get-Item "$candidate").VersionInfo; 
            $ver = $info.ProductVersion; 
            $product = $info.ProductName; 
            $description = "Browser '$product' v$($ver) location is '$candidate'"
            $ret += [pscustomobject] @{ FullPath = $candidate; Version = $ver; Product = $product; Description = $description}
         } catch {}
     }
   }
   @($ret | Sort-Object -Property @{ Expression = { To-Sortable-Version-String $_.Version }; Ascending = $true })
}

function Find-ProgramFiles-Folders() {
  $candidates = @("${Env:ProgramFiles}", "${Env:ProgramFiles(x86)}", "C:\Program Files", "C:\Program Files (x86)", "$ENV:SystemDrive\Program Files", "$ENV:SystemDrive\Program Files (x86)")
  $candidates = @($candidates | Sort-Object | Get-Unique)
  return $candidates
}

function Find-Chrome-Program() {
   $chrome = Find-Chrome-Program-List | Select -First 1
   if (-not $chrome) {
     $chrome = [pscustomobject] @{ FullPath = $null; Version = $null; Product = $null; Description = "Missing Chrome and Chromium (Not Found)" }
   }
   return $chrome
}
# $chrome = Find-Chrome;
# Write-Line -TextMagenta "BROWSER: $((Find-Chrome).Description)"
# Find-Chrome-List | Format-Table -AutoSize | Out-String -Width 1234
function Show-Chrome-Program-List() {
  $chrome_list = @(Find-Chrome-Program-List)
  if ($chrome_list) { 
     Write-Line -TextMagenta "Pre-installed $($chrome_list.Count) chrome or chromium:"; 
     foreach($chrome in $chrome_list) { Write-Line -TextMagenta "  $($chrome.Description)" }
  } Else { 
     Write-Line -TextMagenta "[Show-Chrome-Program-List] Missing any Chrome and Chromium (Not Found)" 
  }
}

function Open-Url-By-Chrome-On-Windows([string] $url) {
   $chrome = Find-Chrome-Program-List | Select -First 1;
   if (-not $chrome.FullPath) {
      Write-Line -TextRed "[Open-Url-By-Chrome] WARNING! Chromium is missing";
      return $false;
   }
   Write-Line -TextCyan "OPENING $url By [$($chrome.Description)] ..."

   # firefox --headless --disable-gpu --no-first-run --no-sandbox https://google.com
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

function Show-Chrome-Processes() {
  if ((Get-Os-Platform) -eq "Windows") {
    $chromes = @(Get-Process | Where-Object { $_.ProcessName -match "chrome" })

    if (-not $chromes) {
      Write-Line -TextRed "No any chrome processes are running";
    } else {
      $megabytes = ($chromes | Measure-Object WorkingSet64 -Sum).Sum / 1MB
      $megabytes = [Math]::Round($megabytes,1)
      Say "Total $($chromes.Count) Chrome Processes are running, total $megabytes MB"
      $chromes | Format-Table -autosize | Out-String -width 123 | Out-Host
    }
  } Else {
    $bash_cmd="ps -aux | awk '`$6 != 0' | grep 'chrome\|chromium\|firefox' | grep -vF 'chrome\|chromium\|firefox'"
    $bash_cmd=@'
          tmp=$(mktemp);
          ps -aux | awk '$6 != 0' > "$tmp";
          cat "$tmp" | grep 'chrome\|chromium\|firefox' | grep -vF 'chrome\|chromium\|firefox' > "${tmp}2"
          total_memory=$(cat "${tmp}2" | awk 'BEGIN {sum = 0} {if ($6 ~ /^[0-9]+(\.[0-9]+)?$/) sum += $6} END {print sum}')
          echo "TOTAL BROWSERS MEMORY: $(Format-Thousand "$total_memory") KB"
          # needs column 2.32+
          (cat "$tmp" | head -1; cat "${tmp}2") | cut -c 1-170
          rm -f "$tmp"* 2>/dev/null
'@
    & bash -c "$bash_cmd"
  }
}

function Kill-Chrome() {
  if ((Get-Os-Platform) -eq "Windows") {
    & taskkill /f /t /im chrome.exe 2>$null
  } Else {
    foreach($cmd_name in @("chromium", "google-chrome", "firefox")) { 
      & bash -c "pkill '$cmd_name' || true"
    }
  }
}

function show-mem() {
   $memDescription = Get-Memory-Info | ForEach-Object { $_.Description }
   Say "Memory: $memDescription"
   Write-Host "CPU: $(Get-Cpu-Name -includeCoreCount)"
   # Write-Line -TextMagenta (Get-Memory-Info).Description
}

function Set-Var {
    param (
        [Parameter(Mandatory=$true)] [string] [AllowEmptyString()] $Name,
        [Parameter(Mandatory=$true)] [string] [AllowEmptyString()] $Value
    )

    try {
        $prev_value = [Environment]::GetEnvironmentVariable($Name)
        Set-Content -Path "Env:$Name" -Value $Value
        if ((Get-Os-Platform) -eq "Windows" ) {
           Set-ItemProperty -Path "HKCU:\Environment" -Name $Name -Value $Value -ErrorAction Stop
        }

        $has_github_env = [bool] ("$env:GITHUB_ENV".Trim().Length -ne 0)
        if ((Is-GITHUB-ACTIONS) -and $has_github_env) {
           $utf8 = New-Object System.Text.UTF8Encoding($false)
           # Write-Host "[Debug] env:GITHUB_ENV is [$env:GITHUB_ENV], Length=$("$env:GITHUB_ENV".Length)"
           # Write-Host "[Debug] utf8 type is [$utf8.GetType()]"
           # Write-Host "[Debug] has_github_env is [$has_github_env]"
           [System.IO.File]::AppendAllText("$env:GITHUB_ENV", "${Name}=${Value}$([Environment]::NewLine)", $utf8)
        }
        if (Is-AZURE_PIPELINE) {
           echo "##vso[task.setvariable variable=${Name};isOutput=true]${Value}"
           echo "##vso[task.setvariable variable=${Name}]${Value}"
        }

        if ("$prev_value" -ne $Value) {
           Write-Line "Env Variable " -TextMagenta "'$Name'" -Reset " set to " -TextGreen "'$Value'"
           $PSNativeCommandArgumentPassing = "Legacy" # does not affect Start-Process
           if ((Get-Os-Platform) -eq "Windows" ) {
               # Start-Process "setx" -ArgumentList @("`"$Name`"", "`"$Value`"") -WindowStyle Hidden # supported by powershell 2.0
               & setx "$Name" "$Value" >$null
           }
        }
    }
    catch {
        Write-Line -TextRed "Failed to write ENV variable '$Name': $($_.Exception.Message)"
    }
}

Function BroadCast-Variables() {
    if ((Get-OS-Platform) -ne "Windows") { return; }
    try {
        # Win32 API Signature
        $signature = '[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
                      public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);'

        # Add type once
        if (-not ([System.Management.Automation.PSTypeName]"Win32.NativeMethods").Type) {
            Add-Type -MemberDefinition $signature -Name "NativeMethods" -Namespace "Win32"
        }

        $result = [UIntPtr]::Zero
        # Ref instead of [out]
        $ret = [Win32.NativeMethods]::SendMessageTimeout([IntPtr]0xffff, 0x001A, [UIntPtr]::Zero, "Environment", 0x02, 1000, [ref]$result)
        
        Write-Line -TextGreen "Broadcast variables success, SendMessageTimeout() --> $ret, [ref] result = $result"
    } 
    catch {
        Write-Line -TextRed "Broadcast variables failed: $($_.Exception.Message)"
    }
}
# BroadCast-Variables
# OK: Bradcast variables success, SendMessageTimeout() --> 1, [ref] result = 0

function Show-Dotnet-And-Chrome-Processes([string] $title) {
  Say "[$title]: DOTNET and CHROME Processes"
  try { 
     Select-WMI-Objects Win32_Process | Select-Object ProcessId, Name, @{Name="WS(MB)"; Expression={[math]::Round($_.WorkingSetSize / 1MB, 1)}}, CommandLine | ? { $_.Name -match "chrome" -or $_.Name -match "dotnet" } | Sort-Object Name | ft -AutoSize | Out-String -width 200
  } catch { Write-Line -TextRed "[Select-WMI-Objects Win32_Process] ERROR: $($_.Exception.Message)" }
}

function Get-OS-Name() {
 if ((Get-OS-Platform) -eq "Windows") {
     $osName = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion" -EA SilentlyContinue | Select-Object -ExpandProperty ProductName
  } Else {
     $cmd = '. /etc/os-release; echo "$PRETTY_NAME"'
     $osName = "$(& bash -c $cmd)"
  }
  return "$osName".Trim()
}

function Show-OS() {
  $is_host = [bool] ("$($ENV:SQL_IMAGE_TAG)" -eq "");
  $is_container = (-not $is_host)
  $kind=If (-not $is_container) { "HOST" } Else { "Container" }
  $osName = Get-OS-Name
  Write-Line -BackBlack -TextMagenta " $($kind) OS: $osName "
}

function Write-Artifact-Info([string] $file, [string] $content) {
  $fullName = Combine-Path "$Env:SYSTEM_ARTIFACTSDIRECTORY" "$file"
  $trimmed="$content".Trim()
  Say "Writing [$trimmed] to [$fullName]"
  [System.IO.File]::WriteAllText($fullName, $trimmed)
}

function Write-SQL-Server-Version-Artifacts() {
    echo "Query SQL Server '$ENV:SQL_INSTANCE_NAME' Medium Version"
    try { 
      $sql_ver = Query-SqlServer-Version -Title "Instance $ENV:SQL_INSTANCE_NAME" -ConnectionString "$ENV:SQLINSIGHTS_CONNECTION_STRING"
      if ($sql_ver) { 
        Write-Line -TextGreen "Query-SqlServer-Version for Medium Version SUCCESS: $sql_ver"
        Write-Artifact-Info "SQL-SERVER-MEDIUM-VERSION.TXT" "$sql_ver" 
      }
    }
    catch {}

    echo "Query SQL Server '$ENV:SQL_INSTANCE_NAME' Title"
    try { 
      $sql_ver = Query-SqlServer-Version -Title "Instance $ENV:SQL_INSTANCE_NAME" -ConnectionString "$ENV:SQLINSIGHTS_CONNECTION_STRING" -Kind "Title"
      if ($sql_ver) {
        Write-Line -TextGreen "Query-SqlServer-Version for Title SUCCESS: $sql_ver"
        Write-Artifact-Info "SQL-SERVER-TITLE.TXT" "$sql_ver"
      }
    }
    catch {}
}


Show-OS

Set-Var "PS1_TROUBLE_SHOOT" "On"
if (Test-Path "D:\") { 
  $sqlMediaFolder = "D:\SQL-Media"; $sqlSetupFolder = "C:\SQL-Setup"; $sqlInstallTo = "D:\SQL"; $root_drive="D:"
} Else {
  $sqlMediaFolder = "C:\SQL-Media"; $sqlSetupFolder = "C:\SQL-Setup"; $sqlInstallTo = "C:\SQL"; $root_drive="C:"
}

Set-Var "SQL_PASSWORD" 'p@assw0rd!'

Set-Var "SQLSERVERS_SETUP_FOLDER" "$sqlSetupFolder"
Set-Var "SQLSERVERS_MEDIA_FOLDER" "$sqlMediaFolder"
Set-Var "SQLSERVERS_INSTALL_TO" "$sqlInstallTo"
Set-Var "PS1_REPO_DOWNLOAD_FOLDER" "C:\Temp-DevOps"
Set-Var "DOTNET_CLI_TELEMETRY_OPTOUT" "1"

$sql_instance_name=if ("$($ENV:SQL_INSTANCE_NAME)") { $ENV:SQL_INSTANCE_NAME } Else { "(local)" }
if ((Get-OS-Platform) -ne "Windows") { $sql_instance_name="127.0.0.1,1433" }
Set-Var "SQL_INSTANCE_NAME" "$sql_instance_name"

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
Set-Var "TEST_SQL_NET_DURATION_OF_Ping" "200"
Set-Var "SENSORSAPP_STRESS_WORKINGSET_ROWS" "150000"
Set-Var "SENSORSAPP_STRESS_DURATION" "2000"

Set-Var "SQLINSIGHTS_REPORT_FOLDER" "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)"
$SQLINSIGHTS_REPORT_FULLNAME = Combine-Path "$($ENV:SQLINSIGHTS_REPORT_FOLDER)" "SqlInsights Report.txt"
Set-Var "SQLINSIGHTS_REPORT_FULLNAME" "$SQLINSIGHTS_REPORT_FULLNAME"

  Say "Setup ErgoFab Tests"

  $sql_security_parameters=if ((Get-OS-Platform) -eq "Windows") { "Integrated Security=SSPI" } Else { "User ID=sa; Password=$($ENV:SQL_PASSWORD)" }
  $NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES=""
  if ( "$env:RAM_DISK" -eq "" ) { $NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES="True" }
  Set-Var "NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES" "$NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES"

  $ERGOFAB_TESTS_DATA_FOLDER = If ((Get-OS-Platform) -eq "Windows") { "$($root_drive)\ergo-fab-tests" } Else { "/mnt/ergo-fab-tests" }
  Set-Var "ERGOFAB_TESTS_DATA_FOLDER" "$ERGOFAB_TESTS_DATA_FOLDER"
  Set-Var "ERGOFAB_TESTS_MASTER_CONNECTIONSTRING" "TrustServerCertificate=True;Data Source=$sql_instance_name;$sql_security_parameters;Encrypt=False;"
  Set-Var "ERGOFAB_TESTS_HISTORY_CONNECTIONSTRING" "Server=$sql_instance_name;Encrypt=False;Initial Catalog=SqlInsights Local Warehouse;$sql_security_parameters;"
  Set-Var "ERGOFAB_TESTS_REPORT_FULLNAME" "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\ErgFab Tests Report.txt"
  $ERGOFAB_SQL_PROVIDER = if ((Get-OS-Platform) -ne "Windows") { "Microsoft" } Else { "System" }
  Set-Var "ERGOFAB_SQL_PROVIDER" "Microsoft"


  $SQLINSIGHTS_DATA_DIR = If ((Get-OS-Platform) -eq "Windows") { "$root_drive\SQL-DATA" } Else { "$ERGOFAB_TESTS_DATA_FOLDER" }
  Set-Var "SQLINSIGHTS_DATA_DIR" "$SQLINSIGHTS_DATA_DIR"

  Say "Setup SQL Storage Tests"
  Set-Var "SQLINSIGHTS_CONNECTION_STRING" "TrustServerCertificate=True;Data Source=$sql_instance_name;$sql_security_parameters;Pooling = true; Encrypt=false"
  Set-Var "TEST_CONFIGURATION" "DISK";
  Set-Var "TEST_CPU_NAME" "$(Get-Cpu-Name -IncludeCoreCount)"
  Set-Var "OS_TITLE" "$(Get-OS-Name)"
  Set-Var "TESTS_FOR_MOT_DISABLED" "False"

  if ((Get-Os-Platform) -eq "Linux") {
     Say "Setup Jam Tests on Linux"
     Set-Var "SQLSERVER_WELLKNOWN_Linux" "$ENV:SQLINSIGHTS_CONNECTION_STRING"
  }



BroadCast-Variables
