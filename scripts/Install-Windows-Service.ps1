param( 
  [string] $DB = "TrustServerCertificate=True;Server=(local);Database=SqlInsights Warehouse;User Id=sa;Password=``1qazxsw2",
  [string] $Compression = "True",
  [string] $CoverItself = "True",
  [string] $ListenOn = "http://*:8080"
)

function To-Boolean($name,$value) {
  if (($value -eq "True") -Or ($value -eq "On") -Or ($value -eq "1")) { return $true; }
  if (($value -eq "False") -Or ($value -eq "Off") -Or ($value -eq "0")) { return $false; }
  Write-Host "Error! Invalid $name parameter '$value'. Boolean parameter accept only True|False|On|Off|1|0" -ForegroundColor Red
  return $false;
}

$Compression = To-Boolean "Compression" $Compression;
$CoverItself = To-Boolean "CoverItself" $CoverItself;

$csb = new-object System.Data.SqlClient.SqlConnectionStringBuilder $DB
$server=$csb.DataSource
$dbName=$csb.InitialCatalog

$archSuffix="x64";
if ($ENV:PROCESSOR_ARCHITECTURE -eq "X86") { $archSuffix="x86" };
if ($ENV:PROCESSOR_ARCHITECTURE -eq "ARM64") { $archSuffix="arm64" };
if ($ENV:PROCESSOR_ARCHITECTURE -eq "ARM") { $archSuffix="arm" };
$file="sqlinsights-dashboard-win-$archSuffix.zip"
$url="https://github.com/devizer/Universe.SqlInsights/releases/latest/download/$file"

function Say-Parameter { param( [string] $name, [string] $value)
    Write-Host "  - $(($name + ":").PadRight(13,[char]32)) '" -NoNewline
    Write-Host "$value" -NoNewline -ForegroundColor Green
    Write-Host "'"
}

Write-Host "Installing SqlInsights Dashboard using parameters:"
Say-Parameter "Compression" $Compression
Say-Parameter "CoverItself" $CoverItself
Say-Parameter "SQL Server" $server
Say-Parameter "Database" $dbName
Say-Parameter "Download" $file
Say-Parameter "Listen On" $ListenOn

$ProgressPreference = 'SilentlyContinue'

$connectionString=$DB;

function Get-Elapsed
{
    if ($Global:startAt -eq $null) { $Global:startAt = [System.Diagnostics.Stopwatch]::StartNew(); }
    [System.String]::Concat("[", (new-object System.DateTime(0)).AddMilliseconds($Global:startAt.ElapsedMilliseconds).ToString("mm:ss"), "]");
}; Get-Elapsed | out-null;

function Say { param( [string] $message )
    Write-Host "$(Get-Elapsed) " -NoNewline -ForegroundColor Magenta
    Write-Host "$message" -ForegroundColor Yellow
}

function Get-Ram() {
    $mem=(Get-CIMInstance Win32_OperatingSystem | Select FreePhysicalMemory,TotalVisibleMemorySize)[0];
    $total=[int] ($mem.TotalVisibleMemorySize / 1024);
    $free=[int] ($mem.FreePhysicalMemory / 1024);
    $info="Total RAM: $($total.ToString("n0")) MB. Free: $($free.ToString("n0")) MB ($([Math]::Round($free * 100 / $total, 1))%)";
    return @{
        Total=$total;
        Free=$free;
        Info=$info;
    }
}

function Get-CPU() {
    return "$((Get-WmiObject Win32_Processor).Name), $([System.Environment]::ProcessorCount) Cores";
}

function Download-File([string] $url, [string]$outfile) {
  [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12;
  $_ = [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($outfile))
  [System.Net.ServicePointManager]::ServerCertificateValidationCallback={$true};
  for ($i=1; $i -le 3; $i++) { 
    $d=new-object System.Net.WebClient; 
    try { 
      $d.DownloadFile("$url","$outfile"); return $true
    } catch { 
      Write-Host $_.Exception -ForegroundColor DarkRed; 
      Write-Host "Try $i of 3 failed for $url" -ForegroundColor DarkRed; 
    } 
  } 
  return $false
}

function Get-Download-Folder() { 
  $temp=$ENV:LOCALAPPDATA
  return [System.IO.Path]::Combine($temp, "SqlInsights Setup Files")
}

function SaveAsJson { 
  param([object]$anObject, [string]$fileName) 
  $unixContent = ($anObject | ConvertTo-Json -Depth 99).Replace("`r", "")
  $Utf8NoBomEncoding = New-Object System.Text.UTF8Encoding $False
  [System.IO.File]::WriteAllLines($fileName, $unixContent, $Utf8NoBomEncoding)
}


Say "CPU: $(Get-CPU)"
Say "$((Get-Ram).Info)"

$zipFile=[System.IO.Path]::Combine((Get-Download-Folder), $file)
Say "Downloading to '$($zipFile)'"
$isDownloadOk = Download-File "$url" $zipFile
Say "Download Completed"

# Delete Existing?
$serviceStatus = [string](Get-Service -Name SqlInsightsDashboard -EA SilentlyContinue).Status
# & sc.exe query SqlInsightsDashboard | out-null; $?
if ($serviceStatus) { 
  if ($serviceStatus -ne "Stopped") {
    Say "Stopping existing SqlInsights Dashboard Service"
    & net.exe stop SqlInsightsDashboard
  }
  Say "Deleting existing SqlInsights Dashboard Service"
  & sc.exe delete SqlInsightsDashboard
}

$installDir="C:\Program Files\SqlInsights Dashboard"
Say "Extracting Archive"
Expand-Archive $zipFile -DestinationPath $installDir -Force
Say "Extract Complete"

Say "Validating System Compatiblity"
$binFullName=[System.IO.Path]::Combine($installDir, "Universe.SqlInsights.W3Api.exe")
$version = (& $binFullName --version | Out-String); $isValid=[bool] $?;
$version = "$version".TrimEnd(@([char]10,[char]13))
if ($isValid) {
  Write-Host "OK. SqlInsights Dashboard Version: $version" -ForegroundColor Green
} else {
  Write-Host "Invalid. Downloaded binaries are not compatible with the current system" -ForegroundColor Red
}

$jsonSettingsFile=[System.IO.Path]::Combine($installDir, "appsettings.json")
$jsonSettings = (Get-Content $jsonSettingsFile) -replace '(?m)\s*//.*?$' -replace '(?ms)/\*.*?\*/' | ConvertFrom-Json
if ($jsonSettings) {
  $jsonSettings.ConnectionStrings.SqlInsights = $connectionString
  $jsonSettings.ResponseCompression = $Compression;
  $jsonSettings.CoverItself = $CoverItself;
  if ($listenOn) {
    $jsonSettings | add-member -Name "ListenOnUrls" -value $listenOn -MemberType NoteProperty
  }
  SaveAsJson $jsonSettings $jsonSettingsFile
} else {
  Write-Host "Unable to parse json setting file '$jsonSettingsFile'" -ForegroundColor Red
}


Say "Creating SqlInsights Dashboard Service"
# Windows 7 Does not Support 'AutomaticDelayedStart'
New-Service -Name SqlInsightsDashboard -BinaryPathName $binFullName -DisplayName "SqlInsights Dashboard" -Description "Provides Web Dashboard and Web API to SqlInsights Storage. Version v$version" -StartupType Automatic
Say "Starting SqlInsights Dashboard Service"
& net.exe start SqlInsightsDashboard
