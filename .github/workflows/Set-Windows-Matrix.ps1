param([string] $SqlSetSize = "MINI", [string] $HostVersion = "2022")
if (-not $SqlSetSize) { $SqlSetSize = "MINI" }
if (-not $HostVersion) { $HostVersion = "2022" }


Import-DevOps

Enumerate-Plain-SQLServer-Downloads | % { [pscustomobject] $_ } | ft -autosize | out-string -width 222 | tee-object "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Plain-SQLServer-Downloads.Table.txt"

$jobs=@()
foreach($meta in Enumerate-Plain-SQLServer-Downloads) { 
  $sql = $meta.NormalizedKeywords
  # LocalDB x86 is not supported on 64-bit windows
  if ($sql -match "LocalDB" -and $sql -match "x86") { continue; }
  # x86 v2012 and x86 v2014 are not supported on Windows 2025 Host
  if ($HostVersion -eq "2025" -and ($sql -like '2012*' -or $sql -like '2014*') -and $sql -match 'x86') { continue; }
  # MINI Amount
  if ($SqlSetSize -eq "MINI") {
      $isMini = [bool] ($sql -match "Core" -and $sql -notmatch "Update")
      $x86_to_skip = "2008-x86 2008R2-x86 2012-x86 2014-x86".Split(" ")
      foreach($skip in $x86_to_skip) { if ($sql -match $skip) { $isMini = $false; } }
      if ($SqlSetSize -eq "MINI" -and (-not $isMini)) { continue; }
  } elseif ($SqlSetSize -eq "LOCALDB") {
      # LocalDB Only
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
  $jobs += [pscustomobject] @{ SQL=$sql; HOST=$run_on; SQL_CONTAINER_SUFFIX=$container_tag }
}

# 2012 and 2014 first
$jobs = @($jobs | Sort-Object @{Expression={$_.SQL -match "2012" -or $_.SQL -match "2014"}; Descending=$true}, @{Expression="SQL"; Descending=$true})


$matrix_object = @{ include = $jobs }
$matrix_string_mini = $matrix_object | ConvertTo-Json -Depth 64 -Compress
$matrix_string_formatted = $matrix_object | ConvertTo-Json -Depth 64

Say "[Size $SqlSetSize on $HostVersion] Github Windows Matrix Formatted-JSON"
Write-Host $matrix_string_formatted

Say "[Size $SqlSetSize on $HostVersion] Github Windows Jobs Table"
$jobs | ft -autosize | out-string -width 222 | tee-object "$($ENV:SYSTEM_ARTIFACTSDIRECTORY)\Windows-Jobs.Table.txt"

Say "[Size $SqlSetSize on $HostVersion] Github Windows Matrix Mini-JSON"
Write-Host $matrix_string_mini

if ($env:GITHUB_OUTPUT) {
  Say "[Size $SqlSetSize on $HostVersion] Generating GITHUB_OUTPUT variable 'matrix'"
  $utf8NoBom = New-Object System.Text.UTF8Encoding $false
  $outputLine = "matrix=$matrix_string_mini" + [System.Environment]::NewLine
  [System.IO.File]::AppendAllText($env:GITHUB_OUTPUT, $outputLine, $utf8NoBom)
}
