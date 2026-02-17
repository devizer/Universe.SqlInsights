param([string] $SqlSetSize = "MINI", [string] $HostVersion = "2022")
if (-not $SqlSetSize) { $SqlSetSize = "MINI" }
if (-not $HostVersion) { $HostVersion = "2022" }

function Create-GitHub-Output-Var([string] $Name, [string] $Value, [switch] $ShowValue) {
  if ($env:GITHUB_OUTPUT) {
     Write-Line -TextMagenta "[Size $SqlSetSize on $HostVersion] Generating GITHUB_OUTPUT variable '$Name'"
     $utf8NoBom = New-Object System.Text.UTF8Encoding $false
     $outputLine = "$Name=$Value" + [System.Environment]::NewLine
     [System.IO.File]::AppendAllText($env:GITHUB_OUTPUT, $outputLine, $utf8NoBom)
     if ($ShowValue) {
        Write-Host "The '$Name' value is below"
        Write-Host $Value
        Write-Host " "
     }
  }
  else { 
    Write-Line -TextRed "[Size $SqlSetSize on $HostVersion] Error! Unable to generate GITHUB_OUTPUT variable '$Name'. Missing env variable GITHUB_OUTPUT"
  }
}

Import-DevOps

Enumerate-Plain-SQLServer-Downloads | % { [pscustomobject] $_ } | ft -autosize | out-string -width 222 | tee-object "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Plain-SQLServer-Downloads.Table.txt"

$jobs_linux=@()
foreach($run_on in "24.04", "22.04") {
foreach($SQL_IMAGE_TAG in "2025", "2022", "2019", "2017") { 
     # on linux SQL is just a title
     $sql = "SQL $SQL_IMAGE_TAG on Ubuntu"
     $container_tag = "$SQL_IMAGE_TAG-latest"
     $jobs_linux += [pscustomobject] @{ SQL=$sql; OS="Ubuntu"; HOST=$run_on; SQL_CONTAINER_SUFFIX=$container_tag }
}
}

$jobs_windows=@()
foreach($meta in Enumerate-Plain-SQLServer-Downloads) { 
  $sql = $meta.NormalizedKeywords
  # LocalDB x86 is not supported on 64-bit windows
  if ($sql -match "LocalDB" -and $sql -match "x86") { continue; }
  # x86 v2012 and x86 v2014 are not supported on Windows 2025 Host
  if ($HostVersion -eq "2025" -and ($sql -like '2012*' -or $sql -like '2014*') -and $sql -match 'x86') { continue; }
  # MINI Set
  if ($SqlSetSize -eq "MINI") {
      $isMini = [bool] ($sql -match "Core" -and $sql -notmatch "Update")
      $x86_to_skip = "2008-x86 2008R2-x86 2012-x86 2014-x86".Split(" ")
      foreach($skip in $x86_to_skip) { if ($sql -match $skip) { $isMini = $false; } }
      if ($SqlSetSize -eq "MINI" -and (-not $isMini)) { continue; }
  } elseif ($SqlSetSize -eq "LOCALDB") {
      # LocalDB Set
      if ($sql -notmatch "LocalDB") { continue; }
  }
  $run_on = "$HostVersion"
  $container_tag = $null
  if ($sql -like '2005*' -or $sql -like '2008*') { $container_tag = "2022" }
  elseif ($sql -like '2012*' -or $sql -like '2014*') { 
    # 2012 and 2014 run on 2022 Host, but need container on 2025
    $container_tag = if ($HostVersion -eq "2025") { "2016" } Else { $null }
  } 
  if ($sql -match 'LocalDB') { $container_tag=$null }
  # Probably we need container for '2017 LocalDB'
  # if ($sql -match '2017 LocalDB') { $container_tag="2022" }
  $jobs_windows += [pscustomobject] @{ SQL=$sql; OS="Windows"; HOST=$run_on; SQL_CONTAINER_SUFFIX=$container_tag }
}

if ($SqlSetSize -eq "FULL") {
  # Update: Because of MINI Set was implemented, sorting is not required
  # 2012 and 2014 first if FULL Set
  # $jobs_windows = @($jobs_windows | Sort-Object @{Expression={$_.SQL -match "2012" -or $_.SQL -match "2014"}; Descending=$true}, @{Expression="SQL"; Descending=$true})
}

$jobs = @( @($jobs_linux) + @($jobs_windows) )
foreach($job in $jobs) {
  Add-Member -InputObject $job -MemberType NoteProperty -Name 'RUNS_ON' -Value "$($job.OS)-$($job.HOST)".ToLower()
}
$matrix_object = @{ include = $jobs }
$matrix_string_mini = $matrix_object | ConvertTo-Json -Depth 64 -Compress
$matrix_string_formatted = $matrix_object | ConvertTo-Json -Depth 64

Say "[Size $SqlSetSize on $HostVersion] Github Windows Matrix Formatted-JSON"
Write-Host $matrix_string_formatted

Say "[Size $SqlSetSize on $HostVersion] Github Windows Jobs Table"
$jobs | ft -autosize | out-string -width 222 | tee-object "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Windows-Jobs.Table.txt"

Say "[Size $SqlSetSize on $HostVersion] Github Windows Matrix Mini-JSON"
Write-Host $matrix_string_mini

Create-GitHub-Output-Var "matrix" "$matrix_string_mini"
Create-GitHub-Output-Var "SQL_SET_SIZE" "$SqlSetSize"
Create-GitHub-Output-Var "HOST_VERSION" "$HostVersion"
