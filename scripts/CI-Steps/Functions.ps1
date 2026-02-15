Import-DevOps


function Is-GITHUB-ACTIONS() { $ENV:GITHUB_ACTIONS -eq "true" }
function Is-AZURE_PIPELINE() { $ENV:TF_BUILD -eq "true" }

function Get-FreePort { $listener = [System.Net.Sockets.TcpListener]0; $listener.Start(); $port = $listener.LocalEndpoint.Port; $listener.Stop(); return $port }

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
           $msg = "SICCESS: `"$exe`" $parameters successfully completed"
           Write-Line -TextGreen $msg
       }
   }
   # return $proc
}

function Find-Chrome-Program-List() {
   $ret = @()
   foreach($candidate in @("C:\Program Files (x86)\Chromium\Application\chrome.exe", "C:\Program Files\Chromium\Application\chrome.exe", "C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", "C:\Program Files\Google\Chrome\Application\chrome.exe")) {
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
     Write-Line -TextMagenta "Missing any Chrome and Chromium (Not Found)" 
  }
}

function Open-Url-By-Chrome-On-Windows([string] $url) {
   $chrome = Find-Chrome-Program-List | Select -Last 1;
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

function Show-Chrome-Processes() {
  $chromes = @(Get-Process | Where-Object { $_.ProcessName -match "chrome" })

  if (-not $chromes) {
    Write-Line -TextRed "No any chrome processes are running";
  } else {
    $megabytes = ($chromes | Measure-Object WorkingSet64 -Sum).Sum / 1MB
    $megabytes = [Math]::Round($megabytes,1)
    Say "Total $($chromes.Count) Chrome Processes are running, total $megabytes MB"
    $chromes | Format-Table -autosize | Out-String -width 123 | Out-Host
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
        [Parameter(Mandatory=$true)] [string] [AllowEmptyString()] $Name,
        [Parameter(Mandatory=$true)] [string] [AllowEmptyString()] $Value
    )

    try {
        $prev_value = [Environment]::GetEnvironmentVariable($Name)
        Set-Content -Path "Env:$Name" -Value $Value
        $registryPath = "HKCU:\Environment"
        Set-ItemProperty -Path $registryPath -Name $Name -Value $Value -ErrorAction Stop

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
           # Start-Process "setx" -ArgumentList @("`"$Name`"", "`"$Value`"") -WindowStyle Hidden # supported by powershell 2.0
           & setx "$Name" "$Value" >$null
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

function Show-Dotnet-And-Chrome-Processes([string] $title) {
  Say "[$title]: DOTNET and CHROME Processes"
  Select-WMI-Objects Win32_Process | Select-Object ProcessId, Name, @{Name="WS(MB)"; Expression={[math]::Round($_.WorkingSetSize / 1MB, 1)}}, CommandLine | ? { $_.Name -match "chrome" -or $_.Name -match "dotnet" } | Sort-Object Name | ft -AutoSize | Out-String -width 200
}

function Get-OS-Name() {
  $osName = Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion" -EA SilentlyContinue | Select-Object -ExpandProperty ProductName
  "$osName".Trim()
}

function Show-OS() {
  $is_host = [bool] ("$($ENV:SQL_IMAGE_TAG)" -eq "");
  $is_container = (-not $is_host)
  $kind=If (-not $is_container) { "HOST" } Else { "Container" }
  $osName = Get-OS-Name
  Write-Line -BackBlack -TextMagenta " $($kind) OS: $osName "
}

function Write-Artifact-Info([string] $file, [string] $content) {
  $fullName="$Env:SYSTEM_ARTIFACTSDIRECTORY\$file"
  $trimmed="$content".Trim()
  Say "Writing [$trimmed] to [$fullName]"
  [System.IO.File]::WriteAllText($fullName, $trimmed)
}


Show-OS

Set-Var "PS1_TROUBLE_SHOOT" "On"
if (Test-Path "D:\") { 
  $sqlMediaFolder = "D:\SQL-Media"; $sqlSetupFolder = "C:\SQL-Setup"; $sqlInstallTo = "D:\SQL"; $root_drive="D:"
} Else {
  $sqlMediaFolder = "C:\SQL-Media"; $sqlSetupFolder = "C:\SQL-Setup"; $sqlInstallTo = "C:\SQL"; $root_drive="C:"
}

Set-Var "SQLSERVERS_SETUP_FOLDER" "$sqlSetupFolder"
Set-Var "SQLSERVERS_MEDIA_FOLDER" "$sqlMediaFolder"
Set-Var "SQLSERVERS_INSTALL_TO" "$sqlInstallTo"
Set-Var "PS1_REPO_DOWNLOAD_FOLDER" "C:\Temp-DevOps"
Set-Var "DOTNET_CLI_TELEMETRY_OPTOUT" "1"

$sql_instance_name=if ("$($ENV:SQL_INSTANCE_NAME)") { $ENV:SQL_INSTANCE_NAME } Else { "(local)" }
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

Set-Var "SQLINSIGHTS_REPORT_FOLDER" "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)"
Set-Var "SQLINSIGHTS_REPORT_FULLNAME" "$($ENV:SQLINSIGHTS_REPORT_FOLDER)\SqlInsights Report.txt"

  Say "Setup ErgoFab Tests"
  $NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES=""
  if ( "$env:RAM_DISK" -eq "" ) { $NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES="True" }
  Set-Var "NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES" "$NUNIT_PIPELINE_KEEP_TEMP_TEST_DATABASES"

  Set-Var "ERGOFAB_TESTS_DATA_FOLDER" "$($root_drive)\ergo-fab-tests"
  Set-Var "ERGOFAB_TESTS_MASTER_CONNECTIONSTRING" "TrustServerCertificate=True;Data Source=$sql_instance_name;Integrated Security=SSPI;Encrypt=False;"
  Set-Var "ERGOFAB_TESTS_HISTORY_CONNECTIONSTRING" "Server=$sql_instance_name;Encrypt=False;Initial Catalog=SqlInsights Local Warehouse;Integrated Security=SSPI;"
  Set-Var "ERGOFAB_TESTS_REPORT_FULLNAME" "$SYSTEM_ARTIFACTSDIRECTORY\ErgFab Tests Report.txt"

  Say "Setup SQL Storage Tests"
  Set-Var "SQLINSIGHTS_CONNECTION_STRING" "TrustServerCertificate=True;Data Source=$sql_instance_name;Integrated Security=SSPI;Pooling = true; Encrypt=false"
  Set-Var "TEST_CONFIGURATION" "DISK";
  Set-Var "TEST_CPU_NAME" "$(Get-Cpu-Name -IncludeCoreCount)"
  Set-Var "OS" "$(Get-OS-Name)"
  Set-Var "TESTS_FOR_MOT_DISABLED" "False"

  # Say "Setup W3API"




BroadCast-Variables
